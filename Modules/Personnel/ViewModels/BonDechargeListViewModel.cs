using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Personnel.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Personnel.ViewModels;

public partial class BonDechargeListViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ILocaleService _locale;
    private readonly IStockMovementService _stock;
    private readonly IStockLocationService _locations;
    private DateTime? _dateFrom, _dateTo;

    public BonDechargeListViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ILocaleService locale,
        IStockMovementService stock,
        IStockLocationService locations)
    {
        _dbFactory = dbFactory;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _locale = locale;
        _stock = stock;
        _locations = locations;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
        Pagination = new PaginationHelper(() => _ = LoadPageAsync(CancellationToken.None));
    }

    public PaginationHelper Pagination { get; }
    public ObservableCollection<BonDechargeListRow> Items { get; } = [];

    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnFilterDate = string.Empty;
    [ObservableProperty] private string _searchWatermark = string.Empty;
    [ObservableProperty] private string _menuDelete = string.Empty;
    [ObservableProperty] private string _colNumero = string.Empty;
    [ObservableProperty] private string _colAssigned = string.Empty;
    [ObservableProperty] private string _colDepot = string.Empty;
    [ObservableProperty] private string _colDate = string.Empty;
    [ObservableProperty] private string _colNote = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private BonDechargeListRow? _selected;

    private void RefreshUi()
    {
        Title = _locale.T("BDH_Title");
        BtnNew = _locale.T("Btn_New");
        BtnFilterDate = _locale.T("Btn_FilterDate");
        SearchWatermark = _locale.T("BDH_SearchWm");
        MenuDelete = _locale.T("Btn_Delete");
        ColNumero = _locale.T("Lbl_ColRef");
        ColAssigned = _locale.T("Col_AssignedTo");
        ColDepot = _locale.T("Col_Depot");
        ColDate = _locale.T("DevisList_ColDate");
        ColNote = _locale.T("Lbl_Note");
        UpdateFilterDateLabel();
    }

    partial void OnSearchTextChanged(string value) => _ = LoadPageAsync(CancellationToken.None, true);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
        => await LoadPageAsync(cancellationToken);

    private async Task LoadPageAsync(CancellationToken ct, bool resetPage = false)
    {
        IsBusy = true;
        try
        {
            if (resetPage) Pagination.CurrentPage = 1;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var q = db.BonsDecharge.AsNoTracking().AsQueryable();
            if (_dateFrom.HasValue) q = q.Where(b => b.Date >= _dateFrom.Value);
            if (_dateTo.HasValue) q = q.Where(b => b.Date <= _dateTo.Value);
            var search = SearchText?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                q = q.Where(b =>
                    EF.Functions.Like(b.Numero, $"%{search}%")
                    || EF.Functions.Like(b.Note, $"%{search}%")
                    || db.Users.AsNoTracking().Any(u => u.Id == b.AssignedToUserId
                        && (EF.Functions.Like(u.FullName, $"%{search}%")
                            || EF.Functions.Like(u.Phone, $"%{search}%"))));
            }

            var total = await q.CountAsync(ct);
            var docs = await q.OrderByDescending(b => b.Date).ThenByDescending(b => b.Id)
                .Skip(Pagination.Skip).Take(Pagination.PageSize)
                .ToListAsync(ct);

            var userIds = docs.Select(d => d.AssignedToUserId).Distinct().ToList();
            var depotIds = docs.Select(d => d.DepotLocationId).Distinct().ToList();
            var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
            var depots = await db.StockLocations.AsNoTracking().Where(l => depotIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.Nom, ct);

            var selId = Selected?.Doc.Id;
            Items.Clear();
            foreach (var d in docs)
            {
                Items.Add(new BonDechargeListRow
                {
                    Doc = d,
                    AssignedToNom = users.GetValueOrDefault(d.AssignedToUserId, "?"),
                    DepotNom = depots.GetValueOrDefault(d.DepotLocationId, "?")
                });
            }
            Pagination.TotalCount = total;
            if (selId is { } id)
                Selected = Items.FirstOrDefault(x => x.Doc.Id == id);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewDoc()
    {
        var vm = _sp.GetRequiredService<BonDechargeEditViewModel>();
        vm.Load(null);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (Selected is null) return;
        var vm = _sp.GetRequiredService<BonDechargeEditViewModel>();
        vm.Load(Selected.Doc.Id);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private async Task FilterDateAsync(CancellationToken cancellationToken)
    {
        var range = await _dialog.PickDateRangeAsync(_locale.T("Btn_FilterDate"), cancellationToken);
        if (range == null) return;
        if (range.Value.from == DateTime.MinValue && range.Value.to == DateTime.MinValue)
        {
            _dateFrom = null;
            _dateTo = null;
        }
        else
        {
            _dateFrom = range.Value.from;
            _dateTo = range.Value.to;
        }
        UpdateFilterDateLabel();
        await LoadPageAsync(cancellationToken, true);
    }

    private void UpdateFilterDateLabel()
    {
        if (_dateFrom == null || _dateTo == null)
            BtnFilterDate = _locale.T("Btn_FilterDate");
        else
            BtnFilterDate = $"{_dateFrom:dd/MM/yy} — {_dateTo:dd/MM/yy}";
    }

    [RelayCommand]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (Selected is null) return;
        if (!await _dialog.ConfirmAsync(_locale.T("BDH_Title"), _locale.T("BDH_ConfirmDelete"), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.BonsDecharge.Include(b => b.Lignes)
                .FirstOrDefaultAsync(b => b.Id == Selected.Doc.Id, cancellationToken);
            if (entity is null) return;

            var user = await db.Users.FirstAsync(u => u.Id == entity.AssignedToUserId, cancellationToken);
            var virtualLoc = await _locations.GetOrCreateVirtualForUserAsync(db, user, cancellationToken);
            await _stock.ResyncBonDechargeStockAsync(
                db, entity.Id, entity.Numero, entity.DepotLocationId, virtualLoc.Id,
                [], null, cancellationToken);
            db.BonsDecharge.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            await LoadPageAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("BDH_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
