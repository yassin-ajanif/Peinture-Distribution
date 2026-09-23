using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Personnel.Services;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Auth.ViewModels;

public partial class VendeursViewModel : BaseViewModel
{
    private readonly IUserService _users;
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockRetrievalService _stockRetrieval;
    private readonly IAppSettingsService _settings;
    private readonly IVendeurCaisseService _caisse;
    private int _soldeLoadToken;

    public VendeursViewModel(
        IUserService users,
        IDialogService dialog,
        ILocaleService locale,
        IDbContextFactory<AppDbContext> dbFactory,
        IStockRetrievalService stockRetrieval,
        IAppSettingsService settings,
        IVendeurCaisseService caisse)
    {
        _users = users;
        _dialog = dialog;
        _locale = locale;
        _dbFactory = dbFactory;
        _stockRetrieval = stockRetrieval;
        _settings = settings;
        _caisse = caisse;
        _locale.CultureApplied += (_, _) => RefreshLabels();
        RefreshLabels();
        Pagination = new PaginationHelper(() => _ = LoadAsync(CancellationToken.None));
    }

    public PaginationHelper Pagination { get; }
    public ObservableCollection<User> Vendeurs { get; } = [];
    public ObservableCollection<VendeurStockLineRow> StockLines { get; } = [];
    public ObservableCollection<VendeurRemiseRow> RemiseLines { get; } = [];
    public IReadOnlyList<ModePaiement> RemiseModes { get; } =
    [
        ModePaiement.Especes,
        ModePaiement.Cheque,
        ModePaiement.TPE,
        ModePaiement.Virement,
        ModePaiement.Effet
    ];

    [ObservableProperty] private string _btnNew = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnDelete = string.Empty;
    [ObservableProperty] private string _wmSearch = string.Empty;
    [ObservableProperty] private string _colNom = string.Empty;
    [ObservableProperty] private string _colPhone = string.Empty;
    [ObservableProperty] private string _colActif = string.Empty;
    [ObservableProperty] private string _lblFiche = string.Empty;
    [ObservableProperty] private string _lblDraftHint = string.Empty;
    [ObservableProperty] private string _lblNom = string.Empty;
    [ObservableProperty] private string _lblPhone = string.Empty;
    [ObservableProperty] private string _lblActif = string.Empty;
    [ObservableProperty] private string _helpList = string.Empty;
    [ObservableProperty] private string _lblSolde = string.Empty;
    [ObservableProperty] private string _lblQtyTotal = string.Empty;
    [ObservableProperty] private string _lblValVenteTtc = string.Empty;
    [ObservableProperty] private string _colRef = string.Empty;
    [ObservableProperty] private string _colDesignation = string.Empty;
    [ObservableProperty] private string _colQty = string.Empty;
    [ObservableProperty] private string _colValVenteTtc = string.Empty;
    [ObservableProperty] private string _emptyStock = string.Empty;
    [ObservableProperty] private string _lblCaisse = string.Empty;
    [ObservableProperty] private string _lblVentes = string.Empty;
    [ObservableProperty] private string _lblEncaisse = string.Empty;
    [ObservableProperty] private string _lblRemis = string.Empty;
    [ObservableProperty] private string _lblARemettre = string.Empty;
    [ObservableProperty] private string _lblRemiseForm = string.Empty;
    [ObservableProperty] private string _lblRemiseDate = string.Empty;
    [ObservableProperty] private string _lblRemiseMode = string.Empty;
    [ObservableProperty] private string _lblRemiseMontant = string.Empty;
    [ObservableProperty] private string _btnSaveRemise = string.Empty;
    [ObservableProperty] private string _btnNewRemise = string.Empty;
    [ObservableProperty] private string _btnDeleteRemise = string.Empty;
    [ObservableProperty] private string _colRemiseNumero = string.Empty;
    [ObservableProperty] private string _colRemiseDate = string.Empty;
    [ObservableProperty] private string _colRemiseMode = string.Empty;
    [ObservableProperty] private string _colRemiseMontant = string.Empty;
    [ObservableProperty] private string _emptyRemises = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private User? _selected;
    [ObservableProperty] private bool _isNewDraft;
    [ObservableProperty] private string _ficheNom = string.Empty;
    [ObservableProperty] private string _fichePhone = string.Empty;
    [ObservableProperty] private bool _ficheActif = true;

    [ObservableProperty] private string _qtyTotalLabel = "0,00";
    [ObservableProperty] private string _valVenteTtcLabel = "—";
    [ObservableProperty] private bool _hasStockLines;

    [ObservableProperty] private string _ventesLabel = "—";
    [ObservableProperty] private string _encaisseLabel = "—";
    [ObservableProperty] private string _remisLabel = "—";
    [ObservableProperty] private string _aRemettreLabel = "—";
    [ObservableProperty] private bool _hasRemiseLines;
    [ObservableProperty] private bool _showRemiseForm;
    [ObservableProperty] private DateTime _remiseDate = DateTime.Today;
    [ObservableProperty] private decimal _remiseMontant;
    [ObservableProperty] private ModePaiement _remiseMode = ModePaiement.Especes;
    [ObservableProperty] private string _remiseNote = string.Empty;
    [ObservableProperty] private VendeurRemiseRow? _selectedRemise;
    [ObservableProperty] private bool _isStockExpanded = true;
    [ObservableProperty] private bool _isCaisseExpanded;

    public bool FicheEditable => (Selected is not null || IsNewDraft) && !IsDepotPrincipalSelected;
    public bool CanDelete => Selected is not null && !IsNewDraft && !IsDepotPrincipalSelected;
    public bool ShowSolde => Selected is not null && !IsNewDraft;
    public bool ShowCaisse => Selected is not null && !IsNewDraft && !IsDepotPrincipalSelected;
    public bool CanDeleteRemise => SelectedRemise is not null;
    public bool ShowFiche => Selected is not null || IsNewDraft;
    public bool IsDepotPrincipalSelected => DbSeeder.IsDepotPrincipalAdmin(Selected);

    private void RefreshLabels()
    {
        Title = _locale.T("Nav_Vendeurs");
        BtnNew = _locale.T("Btn_NewVendeur");
        BtnSave = _locale.T("Btn_Save");
        BtnDelete = _locale.T("Btn_DeleteVendeur");
        WmSearch = _locale.T("Wm_SearchVendeur");
        ColNom = _locale.T("Lbl_ColNom");
        ColPhone = _locale.T("Lbl_ColPhone");
        ColActif = _locale.T("Lbl_ColActif");
        LblFiche = _locale.T("Lbl_VendeurFiche");
        LblDraftHint = _locale.T("Lbl_VendeurDraftHint");
        LblNom = _locale.T("Lbl_FullName");
        LblPhone = _locale.T("Lbl_Phone");
        LblActif = _locale.T("Lbl_Actif");
        HelpList = _locale.T("Lbl_VendeursHelp");
        LblSolde = _locale.T("Lbl_VendeurSolde");
        LblQtyTotal = _locale.T("Lbl_VendeurQtyTotal");
        LblValVenteTtc = _locale.T("Reports_LblStockValVenteTtc");
        ColRef = _locale.T("Lbl_ColRef");
        ColDesignation = _locale.T("Lbl_ColDesignation");
        ColQty = _locale.T("Lbl_ColQty");
        ColValVenteTtc = _locale.T("Reports_LblStockValVenteTtc");
        EmptyStock = _locale.T("Lbl_VendeurSoldeEmpty");
        LblCaisse = _locale.T("Lbl_VendeurCaisse");
        LblVentes = _locale.T("Lbl_VendeurVentes");
        LblEncaisse = _locale.T("Lbl_VendeurEncaisse");
        LblRemis = _locale.T("Lbl_VendeurRemis");
        LblARemettre = _locale.T("Lbl_VendeurARemettre");
        LblRemiseForm = _locale.T("Lbl_VendeurRemiseForm");
        LblRemiseDate = _locale.T("Charges_LblDate");
        LblRemiseMode = _locale.T("Lbl_Mode");
        LblRemiseMontant = _locale.T("Lbl_Montant");
        BtnSaveRemise = _locale.T("Btn_Save");
        BtnNewRemise = _locale.T("Lbl_VendeurRemiseNew");
        BtnDeleteRemise = _locale.T("Btn_Delete");
        ColRemiseNumero = _locale.T("Lbl_ColRef");
        ColRemiseDate = _locale.T("Charges_LblDate");
        ColRemiseMode = _locale.T("Lbl_Mode");
        ColRemiseMontant = _locale.T("Lbl_Montant");
        EmptyRemises = _locale.T("Lbl_VendeurRemisesEmpty");
        LblNote = _locale.T("Lbl_Note");
    }

    partial void OnSearchTextChanged(string value)
    {
        Pagination.CurrentPage = 1;
        _ = LoadAsync(CancellationToken.None);
    }

    partial void OnSelectedChanged(User? value)
    {
        if (value is null)
        {
            if (!IsNewDraft)
                ClearFiche();
            NotifyFicheState();
            return;
        }

        IsNewDraft = false;
        FicheNom = value.FullName;
        FichePhone = value.Phone;
        FicheActif = value.Actif;
        NotifyFicheState();
        _ = LoadSoldeAsync(value.Id, CancellationToken.None);
    }

    partial void OnIsNewDraftChanged(bool value) => NotifyFicheState();

    partial void OnSelectedRemiseChanged(VendeurRemiseRow? value)
    {
        OnPropertyChanged(nameof(CanDeleteRemise));
        DeleteRemiseCommand.NotifyCanExecuteChanged();
    }

    private void NotifyFicheState()
    {
        OnPropertyChanged(nameof(FicheEditable));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(ShowSolde));
        OnPropertyChanged(nameof(ShowCaisse));
        OnPropertyChanged(nameof(ShowFiche));
        OnPropertyChanged(nameof(IsDepotPrincipalSelected));
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var prevId = Selected?.Id;
        IsBusy = true;
        try
        {
            var all = await _users.ListVendeursAsync(SearchText, cancellationToken);
            Pagination.TotalCount = all.Count;
            var page = all
                .Skip(Pagination.Skip)
                .Take(Pagination.PageSize)
                .ToList();

            Vendeurs.Clear();
            foreach (var u in page)
                Vendeurs.Add(u);

            if (prevId is int id)
                Selected = Vendeurs.FirstOrDefault(u => u.Id == id);
            else if (!IsNewDraft)
                ClearFiche();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NewVendeur()
    {
        Selected = null;
        IsNewDraft = true;
        FicheNom = string.Empty;
        FichePhone = string.Empty;
        FicheActif = true;
        ClearSolde();
        NotifyFicheState();
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (IsNewDraft)
            {
                var created = await _users.CreateVendeurAsync(FicheNom, FichePhone, FicheActif, cancellationToken);
                IsNewDraft = false;
                await LoadAsync(cancellationToken);
                Selected = Vendeurs.FirstOrDefault(u => u.Id == created.Id);
                await _dialog.ShowInfoAsync(Title, _locale.T("Vendeur_Created"), cancellationToken);
            }
            else if (Selected is { } sel)
            {
                await _users.UpdateVendeurAsync(sel.Id, FicheNom, FichePhone, FicheActif, cancellationToken);
                var id = sel.Id;
                await LoadAsync(cancellationToken);
                Selected = Vendeurs.FirstOrDefault(u => u.Id == id);
                await _dialog.ShowInfoAsync(Title, _locale.T("Vendeur_Updated"), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(Title, ex.Message, cancellationToken);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (Selected is null || IsNewDraft)
            return;

        var ok = await _dialog.ConfirmAsync(
            Title,
            _locale.Tf("Vendeur_ConfirmDelete", Selected.FullName),
            cancellationToken);
        if (!ok)
            return;

        try
        {
            await _users.DeleteVendeurAsync(Selected.Id, cancellationToken);
            Selected = null;
            ClearFiche();
            await LoadAsync(cancellationToken);
            await _dialog.ShowInfoAsync(Title, _locale.T("Vendeur_Deleted"), cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(Title, ex.Message, cancellationToken);
        }
    }

    private void ClearFiche()
    {
        IsNewDraft = false;
        FicheNom = string.Empty;
        FichePhone = string.Empty;
        FicheActif = true;
        ClearSolde();
        NotifyFicheState();
    }

    private void ClearSolde()
    {
        StockLines.Clear();
        HasStockLines = false;
        QtyTotalLabel = "0,00";
        ValVenteTtcLabel = "—";
        IsStockExpanded = true;
        IsCaisseExpanded = false;
        ClearCaisse();
    }

    private void ClearCaisse()
    {
        RemiseLines.Clear();
        HasRemiseLines = false;
        VentesLabel = "—";
        EncaisseLabel = "—";
        RemisLabel = "—";
        ARemettreLabel = "—";
        ShowRemiseForm = false;
        RemiseDate = DateTime.Today;
        RemiseMontant = 0m;
        RemiseMode = ModePaiement.Especes;
        RemiseNote = string.Empty;
        SelectedRemise = null;
    }

    private async Task LoadSoldeAsync(int userId, CancellationToken cancellationToken)
    {
        var token = ++_soldeLoadToken;
        ClearSolde();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return;

        int? locationId;
        if (DbSeeder.IsDepotPrincipalAdmin(user))
        {
            locationId = await db.StockLocations.AsNoTracking()
                .Where(l => !l.IsVirtual && l.Nom == StockLocation.DefaultDepotNom)
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? await db.StockLocations.AsNoTracking()
                    .Where(l => !l.IsVirtual && l.Actif)
                    .Select(l => (int?)l.Id)
                    .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            locationId = await db.StockLocations.AsNoTracking()
                .Where(l => l.IsVirtual && l.UserId == userId)
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (token != _soldeLoadToken)
            return;

        if (locationId is null)
            return;

        var produitIds = await db.MouvementsStock.AsNoTracking()
            .Where(m => m.FromLocationId == locationId || m.ToLocationId == locationId)
            .Select(m => m.ProduitId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (token != _soldeLoadToken)
            return;

        if (produitIds.Count == 0)
            return;

        var cfg = await _settings.GetAsync(cancellationToken);
        var devise = CurrencyHelper.FromSettings(cfg);
        var currency = string.IsNullOrEmpty(devise) ? "DH" : devise;

        var products = await db.Produits.AsNoTracking()
            .Where(p => produitIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Reference, p.Designation, p.PrixVenteHT, p.TauxTVA })
            .ToListAsync(cancellationToken);

        var stocks = await _stockRetrieval.GetStocksAsync(db, produitIds, locationId.Value, cancellationToken);

        if (token != _soldeLoadToken)
            return;

        decimal qtyTotal = 0m;
        decimal valVenteTtc = 0m;

        foreach (var p in products.OrderBy(x => x.Reference))
        {
            var qty = stocks.GetValueOrDefault(p.Id);
            if (qty == 0m)
                continue;

            var venteTtc = qty * p.PrixVenteHT * (1m + p.TauxTVA / 100m);
            qtyTotal += qty;
            valVenteTtc += venteTtc;

            StockLines.Add(new VendeurStockLineRow
            {
                Reference = p.Reference,
                Designation = p.Designation,
                Quantite = qty,
                ValeurVenteTtc = venteTtc,
                ValeurVenteTtcLabel = CurrencyHelper.Format(venteTtc, currency)
            });
        }

        HasStockLines = StockLines.Count > 0;
        QtyTotalLabel = qtyTotal.ToString("N2");
        ValVenteTtcLabel = CurrencyHelper.Format(valVenteTtc, currency);

        if (token != _soldeLoadToken)
            return;

        if (!DbSeeder.IsDepotPrincipalAdmin(user))
            await LoadCaisseAsync(userId, currency, token, cancellationToken);
    }

    private async Task LoadCaisseAsync(
        int userId,
        string currency,
        int token,
        CancellationToken cancellationToken)
    {
        var summary = await _caisse.GetSummaryAsync(userId, cancellationToken);
        if (token != _soldeLoadToken)
            return;

        VentesLabel = CurrencyHelper.Format(summary.Ventes, currency);
        EncaisseLabel = CurrencyHelper.Format(summary.Encaisse, currency);
        RemisLabel = CurrencyHelper.Format(summary.Remis, currency);
        ARemettreLabel = CurrencyHelper.Format(summary.ARemettre, currency);

        RemiseLines.Clear();
        foreach (var row in summary.Remises)
            RemiseLines.Add(row);

        HasRemiseLines = RemiseLines.Count > 0;
    }

    [RelayCommand]
    private void ToggleStockSection()
    {
        if (IsStockExpanded)
        {
            IsStockExpanded = false;
            return;
        }

        IsStockExpanded = true;
        IsCaisseExpanded = false;
        ShowRemiseForm = false;
    }

    [RelayCommand]
    private void ToggleCaisseSection()
    {
        if (IsCaisseExpanded)
        {
            IsCaisseExpanded = false;
            ShowRemiseForm = false;
            return;
        }

        IsCaisseExpanded = true;
        IsStockExpanded = false;
    }

    [RelayCommand]
    private void ToggleRemiseForm()
    {
        ShowRemiseForm = !ShowRemiseForm;
        if (ShowRemiseForm)
        {
            IsCaisseExpanded = true;
            IsStockExpanded = false;
        }
    }

    [RelayCommand]
    private async Task SaveRemiseAsync(CancellationToken cancellationToken)
    {
        if (Selected is null || IsNewDraft || IsDepotPrincipalSelected)
            return;

        try
        {
            await _caisse.CreateRemiseAsync(
                Selected.Id,
                RemiseDate,
                RemiseMontant,
                RemiseMode,
                RemiseNote,
                cancellationToken);

            ShowRemiseForm = false;
            RemiseMontant = 0m;
            RemiseNote = string.Empty;
            RemiseDate = DateTime.Today;
            RemiseMode = ModePaiement.Especes;

            await LoadSoldeAsync(Selected.Id, cancellationToken);
            await _dialog.ShowInfoAsync(Title, _locale.T("VendeurCaisse_RemiseSaved"), cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(Title, ex.Message, cancellationToken);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteRemise))]
    private async Task DeleteRemiseAsync(CancellationToken cancellationToken)
    {
        if (Selected is null || SelectedRemise is null)
            return;

        var ok = await _dialog.ConfirmAsync(
            Title,
            _locale.Tf("VendeurCaisse_ConfirmDeleteRemise", SelectedRemise.Numero),
            cancellationToken);
        if (!ok)
            return;

        try
        {
            await _caisse.DeleteRemiseAsync(SelectedRemise.Id, cancellationToken);
            SelectedRemise = null;
            await LoadSoldeAsync(Selected.Id, cancellationToken);
            await _dialog.ShowInfoAsync(Title, _locale.T("VendeurCaisse_RemiseDeleted"), cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(Title, ex.Message, cancellationToken);
        }
    }
}
