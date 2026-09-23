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
    private List<VendeurStockLineRow> _sourceStockLines = [];
    private List<VendeurVenteBlRow> _sourceVenteBls = [];
    private List<VendeurEncaisseRow> _sourceEncaissements = [];
    private List<VendeurRemiseRow> _sourceRemises = [];
    private decimal _totalVentes;
    private decimal _totalEncaisse;
    private decimal _totalRemis;
    private string _caisseCurrency = "DH";
    private DateTime? _caisseDateFrom;
    private DateTime? _caisseDateTo;

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
        CaissePagination = new PaginationHelper(ApplyCaisseDetailPage);
        CaissePagination.PageSize = 10;
        StockPagination = new PaginationHelper(ApplyStockPage);
        StockPagination.PageSize = 10;
    }

    public PaginationHelper Pagination { get; }
    public PaginationHelper CaissePagination { get; }
    public PaginationHelper StockPagination { get; }
    public ObservableCollection<User> Vendeurs { get; } = [];
    public ObservableCollection<VendeurStockLineRow> StockLines { get; } = [];
    public ObservableCollection<VendeurRemiseRow> RemiseLines { get; } = [];
    public ObservableCollection<VendeurVenteBlRow> VenteBlLines { get; } = [];
    public ObservableCollection<VendeurEncaisseRow> EncaisseLines { get; } = [];
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
    [ObservableProperty] private string _btnCancelRemise = string.Empty;
    [ObservableProperty] private string _btnNewRemise = string.Empty;
    [ObservableProperty] private string _btnDeleteRemise = string.Empty;
    [ObservableProperty] private string _colRemiseNumero = string.Empty;
    [ObservableProperty] private string _colRemiseDate = string.Empty;
    [ObservableProperty] private string _colRemiseMode = string.Empty;
    [ObservableProperty] private string _colRemiseMontant = string.Empty;
    [ObservableProperty] private string _emptyRemises = string.Empty;
    [ObservableProperty] private string _emptyVentes = string.Empty;
    [ObservableProperty] private string _emptyEncaisse = string.Empty;
    [ObservableProperty] private string _colBlNumero = string.Empty;
    [ObservableProperty] private string _colClient = string.Empty;
    [ObservableProperty] private string _colDate = string.Empty;
    [ObservableProperty] private string _colMontant = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;
    [ObservableProperty] private string _btnFilterCaisseDate = string.Empty;

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
    [ObservableProperty] private bool _hasVenteBlLines;
    [ObservableProperty] private bool _hasEncaisseLines;
    [ObservableProperty] private VendeurCaisseDetailTab _selectedCaisseDetailTab = VendeurCaisseDetailTab.None;
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
    public bool ShowVentesDetail => SelectedCaisseDetailTab == VendeurCaisseDetailTab.Ventes;
    public bool ShowEncaisseDetail => SelectedCaisseDetailTab == VendeurCaisseDetailTab.Encaisse;
    public bool ShowRemisDetail => SelectedCaisseDetailTab == VendeurCaisseDetailTab.Remis;
    public bool IsVentesBadgeSelected => SelectedCaisseDetailTab == VendeurCaisseDetailTab.Ventes;
    public bool IsEncaisseBadgeSelected => SelectedCaisseDetailTab == VendeurCaisseDetailTab.Encaisse;
    public bool IsRemisBadgeSelected => SelectedCaisseDetailTab == VendeurCaisseDetailTab.Remis;
    public bool ShowCaisseDetailToolbar => SelectedCaisseDetailTab != VendeurCaisseDetailTab.None;
    public bool ShowCaisseDateFilter => IsCaisseExpanded;
    public bool ShowCaissePagination => SelectedCaisseDetailTab != VendeurCaisseDetailTab.None && CaissePagination.TotalCount > 0;
    public bool ShowStockPagination => IsStockExpanded && StockPagination.TotalCount > 0;
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
        BtnCancelRemise = _locale.T("Btn_Cancel");
        BtnNewRemise = _locale.T("Lbl_VendeurRemiseNew");
        BtnDeleteRemise = _locale.T("Btn_Delete");
        ColRemiseNumero = _locale.T("Lbl_ColRef");
        ColRemiseDate = _locale.T("Charges_LblDate");
        ColRemiseMode = _locale.T("Lbl_Mode");
        ColRemiseMontant = _locale.T("Lbl_Montant");
        EmptyRemises = _locale.T("Lbl_VendeurRemisesEmpty");
        EmptyVentes = _locale.T("Lbl_VendeurVentesEmpty");
        EmptyEncaisse = _locale.T("Lbl_VendeurEncaisseEmpty");
        ColBlNumero = _locale.T("Lbl_ColRef");
        ColClient = _locale.T("Lbl_ColNom");
        ColDate = _locale.T("Charges_LblDate");
        ColMontant = _locale.T("Lbl_Montant");
        LblNote = _locale.T("Lbl_Note");
        UpdateBtnFilterCaisseDateText();
    }

    partial void OnIsStockExpandedChanged(bool value) =>
        OnPropertyChanged(nameof(ShowStockPagination));

    partial void OnIsCaisseExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowCaisseDateFilter));
        if (value)
            UpdateCaisseChipLabels();
    }

    partial void OnSelectedCaisseDetailTabChanged(VendeurCaisseDetailTab value)
    {
        if (value != VendeurCaisseDetailTab.Remis)
            SelectedRemise = null;

        OnPropertyChanged(nameof(ShowVentesDetail));
        OnPropertyChanged(nameof(ShowEncaisseDetail));
        OnPropertyChanged(nameof(ShowRemisDetail));
        OnPropertyChanged(nameof(IsVentesBadgeSelected));
        OnPropertyChanged(nameof(IsEncaisseBadgeSelected));
        OnPropertyChanged(nameof(IsRemisBadgeSelected));
        OnPropertyChanged(nameof(ShowCaisseDetailToolbar));
        CaissePagination.CurrentPage = 1;
        ApplyCaisseDetailPage();
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
        _sourceStockLines = [];
        HasStockLines = false;
        QtyTotalLabel = "0,00";
        ValVenteTtcLabel = "—";
        StockPagination.Reset(0);
        IsStockExpanded = true;
        IsCaisseExpanded = false;
        ClearCaisse();
    }

    private void ApplyStockPage()
    {
        StockPagination.TotalCount = _sourceStockLines.Count;
        HasStockLines = _sourceStockLines.Count > 0;

        StockLines.Clear();
        foreach (var row in _sourceStockLines.Skip(StockPagination.Skip).Take(StockPagination.PageSize))
            StockLines.Add(row);

        OnPropertyChanged(nameof(ShowStockPagination));
    }

    private void ClearCaisse()
    {
        RemiseLines.Clear();
        VenteBlLines.Clear();
        EncaisseLines.Clear();
        HasRemiseLines = false;
        HasVenteBlLines = false;
        HasEncaisseLines = false;
        SelectedCaisseDetailTab = VendeurCaisseDetailTab.None;
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
        _sourceVenteBls = [];
        _sourceEncaissements = [];
        _sourceRemises = [];
        _totalVentes = 0m;
        _totalEncaisse = 0m;
        _totalRemis = 0m;
        _caisseDateFrom = null;
        _caisseDateTo = null;
        CaissePagination.Reset(0);
        UpdateBtnFilterCaisseDateText();
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
        var stockRows = new List<VendeurStockLineRow>();

        foreach (var p in products.OrderBy(x => x.Reference))
        {
            var qty = stocks.GetValueOrDefault(p.Id);
            if (qty == 0m)
                continue;

            var venteTtc = qty * p.PrixVenteHT * (1m + p.TauxTVA / 100m);
            qtyTotal += qty;
            valVenteTtc += venteTtc;

            stockRows.Add(new VendeurStockLineRow
            {
                Reference = p.Reference,
                Designation = p.Designation,
                Quantite = qty,
                ValeurVenteTtc = venteTtc,
                ValeurVenteTtcLabel = CurrencyHelper.Format(venteTtc, currency)
            });
        }

        _sourceStockLines = stockRows;
        StockPagination.CurrentPage = 1;
        ApplyStockPage();
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

        _caisseCurrency = currency;
        _totalVentes = summary.Ventes;
        _totalEncaisse = summary.Encaisse;
        _totalRemis = summary.Remis;
        _sourceVenteBls = summary.VenteBls.ToList();
        _sourceEncaissements = summary.Encaissements.ToList();
        _sourceRemises = summary.Remises.ToList();

        if (SelectedCaisseDetailTab != VendeurCaisseDetailTab.None)
            ApplyCaisseDetailPage();
        else
            UpdateCaisseChipLabels();
    }

    private void UpdateBtnFilterCaisseDateText()
    {
        if (_caisseDateFrom.HasValue && _caisseDateTo.HasValue)
            BtnFilterCaisseDate = $"{_caisseDateFrom:dd/MM/yy} — {_caisseDateTo:dd/MM/yy}";
        else
            BtnFilterCaisseDate = _locale.T("Btn_FilterDate");
    }

    private bool InCaisseDateRange(DateTime date)
    {
        var d = date.Date;
        if (_caisseDateFrom.HasValue && d < _caisseDateFrom.Value.Date)
            return false;
        if (_caisseDateTo.HasValue && d > _caisseDateTo.Value.Date)
            return false;
        return true;
    }

    private bool HasCaisseDateFilter() => _caisseDateFrom.HasValue && _caisseDateTo.HasValue;

    private bool IsVenteBlInCaisseDateRange(VendeurVenteBlRow row) => InCaisseDateRange(row.Date);

    private bool IsEncaisseInCaisseDateRange(VendeurEncaisseRow row) => InCaisseDateRange(row.BlDate);

    private bool IsRemiseInCaisseDateRange(VendeurRemiseRow row) => InCaisseDateRange(row.Date);

    private void UpdateCaisseChipLabels()
    {
        decimal ventes;
        decimal encaisse;
        decimal remis;

        if (HasCaisseDateFilter())
        {
            ventes = _sourceVenteBls.Where(IsVenteBlInCaisseDateRange).Sum(r => r.Montant);
            encaisse = _sourceEncaissements.Where(IsEncaisseInCaisseDateRange).Sum(r => r.Montant);
            remis = _sourceRemises.Where(IsRemiseInCaisseDateRange).Sum(r => r.Montant);
        }
        else
        {
            ventes = _totalVentes;
            encaisse = _totalEncaisse;
            remis = _totalRemis;
        }

        VentesLabel = CurrencyHelper.Format(ventes, _caisseCurrency);
        EncaisseLabel = CurrencyHelper.Format(encaisse, _caisseCurrency);
        RemisLabel = CurrencyHelper.Format(remis, _caisseCurrency);
        ARemettreLabel = CurrencyHelper.Format(ventes - remis, _caisseCurrency);
    }

    private void ApplyCaisseDetailPage()
    {
        UpdateCaisseChipLabels();

        switch (SelectedCaisseDetailTab)
        {
            case VendeurCaisseDetailTab.Ventes:
                ApplyVenteBlPage();
                break;
            case VendeurCaisseDetailTab.Encaisse:
                ApplyEncaissePage();
                break;
            case VendeurCaisseDetailTab.Remis:
                ApplyRemisePage();
                break;
            default:
                VenteBlLines.Clear();
                EncaisseLines.Clear();
                RemiseLines.Clear();
                HasVenteBlLines = false;
                HasEncaisseLines = false;
                HasRemiseLines = false;
                CaissePagination.TotalCount = 0;
                break;
        }

        OnPropertyChanged(nameof(ShowCaissePagination));
    }

    private void ApplyVenteBlPage()
    {
        var filtered = _sourceVenteBls.Where(IsVenteBlInCaisseDateRange).ToList();
        CaissePagination.TotalCount = filtered.Count;
        HasVenteBlLines = filtered.Count > 0;

        VenteBlLines.Clear();
        foreach (var row in filtered.Skip(CaissePagination.Skip).Take(CaissePagination.PageSize))
            VenteBlLines.Add(row);
    }

    private void ApplyEncaissePage()
    {
        var filtered = _sourceEncaissements.Where(IsEncaisseInCaisseDateRange).ToList();
        CaissePagination.TotalCount = filtered.Count;
        HasEncaisseLines = filtered.Count > 0;

        EncaisseLines.Clear();
        foreach (var row in filtered.Skip(CaissePagination.Skip).Take(CaissePagination.PageSize))
            EncaisseLines.Add(row);
    }

    private void ApplyRemisePage()
    {
        var filtered = _sourceRemises.Where(IsRemiseInCaisseDateRange).ToList();
        CaissePagination.TotalCount = filtered.Count;
        HasRemiseLines = filtered.Count > 0;

        var selectedId = SelectedRemise?.Id;
        RemiseLines.Clear();
        foreach (var row in filtered.Skip(CaissePagination.Skip).Take(CaissePagination.PageSize))
            RemiseLines.Add(row);

        if (selectedId is int id)
            SelectedRemise = RemiseLines.FirstOrDefault(r => r.Id == id);
    }

    [RelayCommand]
    private async Task FilterCaisseDateAsync(CancellationToken cancellationToken)
    {
        var range = await _dialog.PickDateRangeAsync(_locale.T("Btn_FilterDate"), cancellationToken);
        if (range == null)
            return;

        if (range.Value.from == DateTime.MinValue && range.Value.to == DateTime.MinValue)
        {
            _caisseDateFrom = null;
            _caisseDateTo = null;
        }
        else
        {
            _caisseDateFrom = range.Value.from;
            _caisseDateTo = range.Value.to;
        }

        UpdateBtnFilterCaisseDateText();
        CaissePagination.CurrentPage = 1;
        UpdateCaisseChipLabels();
        ApplyCaisseDetailPage();
    }

    [RelayCommand]
    private void SelectCaisseVentes() => ToggleCaisseDetail(VendeurCaisseDetailTab.Ventes);

    [RelayCommand]
    private void SelectCaisseEncaisse() => ToggleCaisseDetail(VendeurCaisseDetailTab.Encaisse);

    [RelayCommand]
    private void SelectCaisseRemis() => ToggleCaisseDetail(VendeurCaisseDetailTab.Remis);

    private void ToggleCaisseDetail(VendeurCaisseDetailTab tab)
    {
        IsCaisseExpanded = true;
        IsStockExpanded = false;
        ShowRemiseForm = false;
        SelectedCaisseDetailTab = SelectedCaisseDetailTab == tab
            ? VendeurCaisseDetailTab.None
            : tab;
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
            SelectedCaisseDetailTab = VendeurCaisseDetailTab.None;
            return;
        }

        IsCaisseExpanded = true;
        IsStockExpanded = false;
    }

    [RelayCommand]
    private void ToggleRemiseForm()
    {
        if (ShowRemiseForm)
        {
            CloseRemiseForm();
            return;
        }

        ShowRemiseForm = true;
        IsCaisseExpanded = true;
        IsStockExpanded = false;
    }

    [RelayCommand]
    private void CloseRemiseForm()
    {
        ShowRemiseForm = false;
        RemiseMontant = 0m;
        RemiseNote = string.Empty;
        RemiseDate = DateTime.Today;
        RemiseMode = ModePaiement.Especes;
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
