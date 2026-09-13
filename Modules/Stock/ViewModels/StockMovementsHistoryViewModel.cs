using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.AvoirFournisseur.ViewModels;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.Livraison.ViewModels;
using GestionCommerciale.Modules.Preparation.ViewModels;
using GestionCommerciale.Modules.Reception.ViewModels;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Stock.ViewModels;

public partial class StockMovementsHistoryViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly ILocaleService _locale;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;

    private int _produitId;
    private string _productLabel = string.Empty;

    public StockMovementsHistoryViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        ILocaleService locale,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _locale = locale;
        _workspace = workspaceNavigator;
        _sp = sp;
        _locale.CultureApplied += (_, _) => RefreshUi();
        Pagination = new PaginationHelper(() => _ = LoadMouvementsAsync(CancellationToken.None));
        RefreshUi();
    }

    public Action? RequestClose { get; set; }

    public PaginationHelper Pagination { get; }

    public ObservableCollection<MouvementStock> Mouvements { get; } = [];

    [ObservableProperty] private string _productTitle = string.Empty;
    [ObservableProperty] private string _movementClientSearch = string.Empty;
    [ObservableProperty] private string _colDate = string.Empty;
    [ObservableProperty] private string _colType = string.Empty;
    [ObservableProperty] private string _colFrom = string.Empty;
    [ObservableProperty] private string _colTo = string.Empty;
    [ObservableProperty] private string _colStockCurrent = string.Empty;
    [ObservableProperty] private string _colQty = string.Empty;
    [ObservableProperty] private string _colDetail = string.Empty;
    [ObservableProperty] private string _wmMovementClientSearch = string.Empty;
    [ObservableProperty] private string _btnClose = string.Empty;

    public void Configure(int produitId, string productLabel)
    {
        _produitId = produitId;
        _productLabel = productLabel;
        Pagination.CurrentPage = 1;
        MovementClientSearch = string.Empty;
        Mouvements.Clear();
        RefreshUi();
    }

    private void RefreshUi()
    {
        ProductTitle = string.IsNullOrWhiteSpace(_productLabel)
            ? _locale.T("Lbl_MovementsForProduct")
            : _locale.Tf("Stock_HistoryTitle", _productLabel);
        ColDate = _locale.T("Lbl_ColDate");
        ColType = _locale.T("Lbl_ColTypeMvt");
        ColFrom = _locale.T("Lbl_ColStockFrom");
        ColTo = _locale.T("Lbl_ColStockTo");
        ColStockCurrent = _locale.T("Lbl_ColStockCurrent");
        ColQty = _locale.T("Lbl_ColQty");
        ColDetail = _locale.T("Lbl_ColDetail");
        WmMovementClientSearch = _locale.T("Wm_SearchMovementClient");
        BtnClose = _locale.T("Btn_Back");
        if (_produitId != 0)
            _ = LoadMouvementsAsync(CancellationToken.None);
    }

    partial void OnMovementClientSearchChanged(string value)
    {
        if (_produitId == 0) return;
        Pagination.CurrentPage = 1;
        _ = LoadMouvementsAsync(CancellationToken.None);
    }

    [RelayCommand]
    private async Task LoadMouvementsAsync(CancellationToken cancellationToken)
    {
        if (_produitId == 0) return;
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var q = db.MouvementsStock.AsNoTracking()
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Where(m => m.ProduitId == _produitId)
            .WherePartyNameMatches(db, MovementClientSearch);
        var total = await q.CountAsync(cancellationToken);
        var list = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip(Pagination.Skip)
            .Take(Pagination.PageSize)
            .ToListAsync(cancellationToken);
        await MouvementStockEnricher.EnrichMovementDetailsAsync(db, list, _locale.T("Lbl_PrixHt"), cancellationToken);
        MouvementStockEnricher.ApplyLocationLabels(list, _locale);
        Mouvements.Clear();
        foreach (var m in list) Mouvements.Add(m);
        Pagination.TotalCount = total;
    }

    [RelayCommand]
    private void OpenOriginDocument(MouvementStock? mouvement)
    {
        if (mouvement?.OrigineId is not int id || !mouvement.CanOpenOrigin) return;

        switch (mouvement.OrigineType)
        {
            case StockMovementService.OrigineTypeBonLivraison:
            {
                var vm = _sp.GetRequiredService<BLEditViewModel>();
                vm.Load(id);
                _workspace.Open(vm);
                break;
            }
            case StockMovementService.OrigineTypeBonPreparation:
            {
                var vm = _sp.GetRequiredService<BonPreparationEditViewModel>();
                vm.Load(id);
                _workspace.Open(vm);
                break;
            }
            case StockMovementService.OrigineTypeBonReception:
            {
                var vm = _sp.GetRequiredService<BREditViewModel>();
                vm.Load(id);
                _workspace.Open(vm);
                break;
            }
            case StockMovementService.OrigineTypeAvoir:
            {
                var vm = _sp.GetRequiredService<AvoirEditViewModel>();
                vm.Load(id);
                _workspace.Open(vm);
                break;
            }
            case StockMovementService.OrigineTypeAvoirFournisseur:
            {
                var vm = _sp.GetRequiredService<AvoirFournisseurEditViewModel>();
                vm.Load(id);
                _workspace.Open(vm);
                break;
            }
        }

        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Close() => RequestClose?.Invoke();
}
