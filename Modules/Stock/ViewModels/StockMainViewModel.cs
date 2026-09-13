using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Stock.Views;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Stock.ViewModels;

public partial class StockMainViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockMovementService _stock;
    private readonly IDialogService _dialog;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IServiceProvider _sp;

    public StockMainViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IStockMovementService stock,
        IDialogService dialog,
        ICurrentUserSession session,
        ILocaleService locale,
        IServiceProvider sp)
    {
        _dbFactory = dbFactory;
        _stock = stock;
        _dialog = dialog;
        _session = session;
        _locale = locale;
        _sp = sp;
        _locale.CultureApplied += (_, _) => RefreshStockUi();
        RefreshStockUi();
        Pagination = new PaginationHelper(() => _ = LoadProduitsAsync(CancellationToken.None));
    }

    public PaginationHelper Pagination { get; }

    [ObservableProperty] private string _lblCatalog = string.Empty;
    [ObservableProperty] private string _helpStock = string.Empty;
    [ObservableProperty] private string _wmSearch = string.Empty;
    [ObservableProperty] private string _colRef = string.Empty;
    [ObservableProperty] private string _colDesignation = string.Empty;
    [ObservableProperty] private string _colStock = string.Empty;
    [ObservableProperty] private string _colMinDot = string.Empty;
    [ObservableProperty] private string _lblAdjustManual = string.Empty;
    [ObservableProperty] private string _lblVariation = string.Empty;
    [ObservableProperty] private string _lblMotifTrace = string.Empty;
    [ObservableProperty] private string _wmAdjustNote = string.Empty;
    [ObservableProperty] private string _btnApply = string.Empty;
    [ObservableProperty] private string _btnHistory = string.Empty;

    public bool CanOpenHistory => SelectedProduit != null;

    private void RefreshStockUi()
    {
        Title = _locale.T("Stock_Title");
        LblCatalog = _locale.T("Lbl_Catalog");
        HelpStock = _locale.T("Lbl_StockMainHelp");
        WmSearch = _locale.T("Wm_SearchProducts");
        ColRef = _locale.T("Lbl_ColRef");
        ColDesignation = _locale.T("Lbl_ColDesignation");
        ColStock = _locale.T("Lbl_ColStock");
        ColMinDot = _locale.T("Lbl_ColMinDot");
        LblAdjustManual = _locale.T("Lbl_AdjustDelta");
        LblVariation = _locale.T("Lbl_Variation");
        LblMotifTrace = _locale.T("Lbl_MotifTrace");
        WmAdjustNote = _locale.T("Wm_AdjustNote");
        BtnApply = _locale.T("Btn_Apply");
        BtnHistory = _locale.T("Btn_StockHistory");
    }

    partial void OnProductSearchChanged(string value)
    {
        Pagination.CurrentPage = 1;
        _ = LoadProduitsAsync(CancellationToken.None);
    }

    public ObservableCollection<Produit> Produits { get; } = [];

    [ObservableProperty] private Produit? _selectedProduit;

    [ObservableProperty] private string _productSearch = string.Empty;

    [ObservableProperty] private decimal _ajustementDelta;
    [ObservableProperty] private string _ajustementNote = string.Empty;

    [RelayCommand]
    private async Task LoadProduitsAsync(CancellationToken cancellationToken)
    {
        var prevId = SelectedProduit?.Id;
        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var q = db.Produits.AsNoTracking()
                .WhereSearchMatches(ProductSearch)
                .SelectForListWithoutImageData();
            var total = await q.CountAsync(cancellationToken);
            var list = await q
                .OrderBy(p => p.Reference)
                .Skip(Pagination.Skip).Take(Pagination.PageSize)
                .ToListAsync(cancellationToken);
            await StockBalanceQueries.HydrateStockActuelAsync(db, list, cancellationToken);
            Produits.Clear();
            foreach (var p in list) Produits.Add(p);
            Pagination.TotalCount = total;
            if (prevId.HasValue)
                SelectedProduit = Produits.FirstOrDefault(p => p.Id == prevId.Value);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedProduitChanged(Produit? value)
    {
        OnPropertyChanged(nameof(CanOpenHistory));
        OpenHistoryCommand.NotifyCanExecuteChanged();
        AjustementDelta = 0;
        AjustementNote = string.Empty;
    }

    [RelayCommand]
    private async Task AjustementAsync(CancellationToken cancellationToken)
    {
        if (SelectedProduit == null) return;
        if (AjustementDelta == 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("Stock_Title"), _locale.T("Stock_ErrVariation"), cancellationToken);
            return;
        }

        var id = SelectedProduit.Id;
        var libInventaire = _locale.T("Stock_DefaultMotif");
        var motif = AjustementNote.Trim();
        var detailNote = string.IsNullOrEmpty(motif)
            ? libInventaire
            : $"{libInventaire} — {motif}";
        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);
            await _stock.ApplyMovementAsync(
                db,
                id,
                TypeMouvement.Ajustement,
                AjustementDelta,
                libInventaire,
                null,
                detailNote,
                _session.UserId,
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await trx.CommitAsync(cancellationToken);
            AjustementDelta = 0;
            AjustementNote = string.Empty;
            await LoadProduitsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("Stock_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenHistory))]
    private async Task OpenHistoryAsync(CancellationToken cancellationToken)
    {
        if (SelectedProduit is null) return;

        var vm = _sp.GetRequiredService<StockMovementsHistoryViewModel>();
        vm.Configure(SelectedProduit.Id, $"{SelectedProduit.Reference} — {SelectedProduit.Designation}");
        await StockMovementsHistoryHost.ShowAsync(vm, cancellationToken);
    }
}
