using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.AvoirFournisseur.ViewModels;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Charges.ViewModels;
using GestionCommerciale.Modules.Devis.ViewModels;
using GestionCommerciale.Modules.Facturation.ViewModels;
using GestionCommerciale.Modules.FactureFournisseur.ViewModels;
using GestionCommerciale.Modules.Livraison.ViewModels;
using GestionCommerciale.Modules.CommandeFournisseur.ViewModels;
using GestionCommerciale.Modules.CommandeClient.ViewModels;
using GestionCommerciale.Modules.Personnel.ViewModels;
using GestionCommerciale.Modules.Reception.ViewModels;
using GestionCommerciale.Modules.Reporting.ViewModels;
using GestionCommerciale.Modules.Stock.ViewModels;
using GestionCommerciale.Modules.Tiers.Models;
using GestionCommerciale.Modules.Tiers.ViewModels;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Auth.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly PerformanceTestService _testService;

    public AppShellViewModel(
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        PerformanceTestService testService)
    {
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _testService = testService;
        UserLabel = session.Nom ?? string.Empty;
        _workspace.CurrentPageChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(WorkspaceCurrentPage));
            UpdateActiveNav();
        };
        _locale.CultureApplied += (_, _) => RefreshShellLabels();
        RefreshShellLabels();
        _workspace.Open(_sp.GetRequiredService<HomeViewModel>());
        UpdateActiveNav();
    }

    public BaseViewModel? WorkspaceCurrentPage => _workspace.CurrentPage;

    [ObservableProperty] private string _userLabel = string.Empty;

    [ObservableProperty] private string _navHome = string.Empty;
    [ObservableProperty] private string _navDistribution = string.Empty;
    [ObservableProperty] private string _navBonCharge = string.Empty;
    [ObservableProperty] private string _navBonDecharge = string.Empty;
    [ObservableProperty] private string _navVente = string.Empty;
    [ObservableProperty] private string _navAchat = string.Empty;
    [ObservableProperty] private string _navClients = string.Empty;
    [ObservableProperty] private string _navDevis = string.Empty;
    [ObservableProperty] private string _navBcc = string.Empty;
    [ObservableProperty] private string _navBl = string.Empty;
    [ObservableProperty] private string _navFactures = string.Empty;
    [ObservableProperty] private string _navAvoirs = string.Empty;
    [ObservableProperty] private string _navAvoirFournisseur = string.Empty;
    [ObservableProperty] private string _navCharges = string.Empty;
    [ObservableProperty] private string _navFournisseurs = string.Empty;
    [ObservableProperty] private string _navBc = string.Empty;
    [ObservableProperty] private string _navBr = string.Empty;
    [ObservableProperty] private string _navFacturesFournisseur = string.Empty;
    [ObservableProperty] private string _navStockAdmin = string.Empty;
    [ObservableProperty] private string _navStock = string.Empty;
    [ObservableProperty] private string _navProduits = string.Empty;
    [ObservableProperty] private string _navVendeurs = string.Empty;
    [ObservableProperty] private string _navReports = string.Empty;
    [ObservableProperty] private string _navSettings = string.Empty;

    [ObservableProperty] private bool _isTestRunning;
    [ObservableProperty] private string _testProgress = string.Empty;

    [ObservableProperty] private bool _isNavHomeActive;
    [ObservableProperty] private bool _isNavBonChargeActive;
    [ObservableProperty] private bool _isNavBonDechargeActive;
    [ObservableProperty] private bool _isNavClientsActive;
    [ObservableProperty] private bool _isNavFournisseursActive;
    [ObservableProperty] private bool _isNavDevisActive;
    [ObservableProperty] private bool _isNavBccActive;
    [ObservableProperty] private bool _isNavBlActive;
    [ObservableProperty] private bool _isNavFacturesActive;
    [ObservableProperty] private bool _isNavAvoirsActive;
    [ObservableProperty] private bool _isNavAvoirFournisseurActive;
    [ObservableProperty] private bool _isNavChargesActive;
    [ObservableProperty] private bool _isNavBcActive;
    [ObservableProperty] private bool _isNavBrActive;
    [ObservableProperty] private bool _isNavFacturesFournisseurActive;
    [ObservableProperty] private bool _isNavStockActive;
    [ObservableProperty] private bool _isNavProduitsActive;
    [ObservableProperty] private bool _isNavVendeursActive;
    [ObservableProperty] private bool _isNavReportsActive;
    [ObservableProperty] private bool _isNavSettingsActive;

    private void RefreshShellLabels()
    {
        NavHome = _locale.T("Nav_Home");
        NavDistribution = _locale.T("Nav_Distribution");
        NavBonCharge = _locale.T("Nav_BonCharge");
        NavBonDecharge = _locale.T("Nav_BonDecharge");
        NavVente = _locale.T("Nav_Vente");
        NavAchat = _locale.T("Nav_Achat");
        NavClients = _locale.T("Nav_Clients");
        NavDevis = _locale.T("Nav_Devis");
        NavBcc = _locale.T("Nav_BCC");
        NavBl = _locale.T("Nav_BL");
        NavFactures = _locale.T("Nav_Factures");
        NavAvoirs = _locale.T("Nav_Avoirs");
        NavAvoirFournisseur = _locale.T("Nav_AvoirFournisseur");
        NavCharges = _locale.T("Nav_Charges");
        NavFournisseurs = _locale.T("Nav_Fournisseurs");
        NavBc = _locale.T("Nav_BC");
        NavBr = _locale.T("Nav_BR");
        NavFacturesFournisseur = _locale.T("Nav_FacturesFournisseur");
        NavStockAdmin = _locale.T("Nav_StockAdmin");
        NavStock = _locale.T("Nav_Stock");
        NavProduits = _locale.T("Nav_Produits");
        NavVendeurs = _locale.T("Nav_Vendeurs");
        NavReports = _locale.T("Nav_Reports");
        NavSettings = _locale.T("Nav_Settings");
        Title = NavHome;
    }

    [ObservableProperty] private bool _distributionNavExpanded;
    [ObservableProperty] private bool _venteNavExpanded;
    [ObservableProperty] private bool _achatNavExpanded;
    [ObservableProperty] private bool _footerNavExpanded;

    public string DistributionNavArrow => DistributionNavExpanded ? "\u25BC" : "\u25B6";
    public string VenteNavArrow => VenteNavExpanded ? "\u25BC" : "\u25B6";
    public string AchatNavArrow => AchatNavExpanded ? "\u25BC" : "\u25B6";
    public string FooterNavArrow => FooterNavExpanded ? "\u25BC" : "\u25B6";

    partial void OnDistributionNavExpandedChanged(bool value) => OnPropertyChanged(nameof(DistributionNavArrow));
    partial void OnVenteNavExpandedChanged(bool value) => OnPropertyChanged(nameof(VenteNavArrow));
    partial void OnAchatNavExpandedChanged(bool value) => OnPropertyChanged(nameof(AchatNavArrow));
    partial void OnFooterNavExpandedChanged(bool value) => OnPropertyChanged(nameof(FooterNavArrow));

    [RelayCommand]
    private void ToggleDistributionNav()
    {
        var open = !DistributionNavExpanded;
        DistributionNavExpanded = open;
        if (open)
        {
            VenteNavExpanded = false;
            AchatNavExpanded = false;
            FooterNavExpanded = false;
        }
    }

    [RelayCommand]
    private void ToggleVenteNav()
    {
        var open = !VenteNavExpanded;
        VenteNavExpanded = open;
        if (open)
        {
            DistributionNavExpanded = false;
            AchatNavExpanded = false;
            FooterNavExpanded = false;
        }
    }

    [RelayCommand]
    private void ToggleAchatNav()
    {
        var open = !AchatNavExpanded;
        AchatNavExpanded = open;
        if (open)
        {
            DistributionNavExpanded = false;
            VenteNavExpanded = false;
            FooterNavExpanded = false;
        }
    }

    [RelayCommand]
    private void ToggleFooterNav()
    {
        var open = !FooterNavExpanded;
        FooterNavExpanded = open;
        if (open)
        {
            DistributionNavExpanded = false;
            VenteNavExpanded = false;
            AchatNavExpanded = false;
        }
    }

    public bool ShowNavClients => _session.CanAccessClients;
    public bool ShowNavFournisseurs => _session.CanAccessFournisseurs;
    public bool ShowNavStock => _session.CanAccessStock;
    public bool ShowNavProduits => _session.CanAccessStock;
    public bool ShowNavVendeurs => _session.CanAccessUsers;
    public bool ShowNavBonCharge => _session.CanAccessStock;
    public bool ShowNavBonDecharge => _session.CanAccessStock;
    public bool ShowNavDevis => _session.CanAccessDevis;
    public bool ShowNavBCC => _session.CanAccessDevis;
    public bool ShowNavBL => _session.CanAccessBL;
    public bool ShowNavBR => _session.CanAccessBR;
    public bool ShowNavBC => _session.CanAccessBC;
    public bool ShowNavFactures => _session.CanAccessFacturation;
    public bool ShowNavAvoirs => _session.CanAccessAvoir;
    public bool ShowNavFacturesFournisseur => _session.CanAccessFacturation;
    public bool ShowNavAvoirFournisseur => _session.CanAccessAvoir;
    public bool ShowNavCharges => _session.CanAccessCharges;
    public bool ShowNavReports => _session.CanAccessReporting;
    public bool ShowNavSettings => _session.CanAccessSettings;

    [RelayCommand]
    private void GoHome() => _workspace.Open(_sp.GetRequiredService<HomeViewModel>());

    [RelayCommand]
    private void GoClients()
    {
        var vm = _sp.GetRequiredService<TiersListViewModel>();
        vm.Configure(TiersListScope.Clients);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void GoFournisseurs()
    {
        var vm = _sp.GetRequiredService<TiersListViewModel>();
        vm.Configure(TiersListScope.Fournisseurs);
        _workspace.Open(vm);
    }

    [RelayCommand]
    private void GoStock() => _workspace.Open(_sp.GetRequiredService<StockMainViewModel>());

    [RelayCommand]
    private void GoProduits() => _workspace.Open(_sp.GetRequiredService<ProduitsViewModel>());

    [RelayCommand]
    private void GoVendeurs() => _workspace.Open(_sp.GetRequiredService<VendeursViewModel>());

    [RelayCommand]
    private void GoBonCharge() => _workspace.Open(_sp.GetRequiredService<BonChargeListViewModel>());

    [RelayCommand]
    private void GoBonDecharge() => _workspace.Open(_sp.GetRequiredService<BonDechargeListViewModel>());

    [RelayCommand]
    private void GoReports()
    {
        var vm = _sp.GetRequiredService<ReportsListViewModel>();
        _workspace.Open(vm);
        vm.GoProfitChargesCommand.Execute(null);
    }

    [RelayCommand]
    private void GoDevis() => _workspace.Open(_sp.GetRequiredService<DevisListViewModel>());

    [RelayCommand]
    private void GoBCV()
    {
        var vm = _sp.GetRequiredService<BCVListViewModel>();
        _workspace.Open(vm);
        vm.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private void GoBL() => _workspace.Open(_sp.GetRequiredService<BLListViewModel>());

    [RelayCommand]
    private void GoBR() => _workspace.Open(_sp.GetRequiredService<BRListViewModel>());

    [RelayCommand]
    private void GoBC()
    {
        var vm = _sp.GetRequiredService<BCListViewModel>();
        _workspace.Open(vm);
        vm.LoadCommand.Execute(null);
    }

    [RelayCommand]
    private void GoFactures() => _workspace.Open(_sp.GetRequiredService<FactureListViewModel>());

    [RelayCommand]
    private void GoAvoirs() => _workspace.Open(_sp.GetRequiredService<AvoirListViewModel>());

    [RelayCommand]
    private void GoFacturesFournisseur() => _workspace.Open(_sp.GetRequiredService<FactureFournisseurListViewModel>());

    [RelayCommand]
    private void GoAvoirFournisseur() => _workspace.Open(_sp.GetRequiredService<AvoirFournisseurListViewModel>());

    [RelayCommand]
    private void GoCharges() => _workspace.Open(_sp.GetRequiredService<ChargeListViewModel>());

    [RelayCommand]
    private void GoSettings() => _workspace.Open(_sp.GetRequiredService<SettingsViewModel>());

    [RelayCommand]
    private async Task RunPerfTestAsync(CancellationToken ct)
    {
        if (IsTestRunning) return;
        IsTestRunning = true;
        TestProgress = string.Empty;
        try
        {
            var progress = new Progress<string>(msg => TestProgress = msg);
            var result = await _testService.RunAsync(progress, ct);
            TestProgress = result;
        }
        finally
        {
            IsTestRunning = false;
        }
    }

    private void UpdateActiveNav()
    {
        var p = _workspace.CurrentPage;
        IsNavHomeActive = p is HomeViewModel;
        IsNavClientsActive = p is TiersListViewModel tl && tl.Scope == TiersListScope.Clients
            || p is TiersDetailViewModel td && td.ListScope == TiersListScope.Clients;
        IsNavFournisseursActive = p is TiersListViewModel tiersList && tiersList.Scope == TiersListScope.Fournisseurs
            || p is TiersDetailViewModel tiersDetail && tiersDetail.ListScope == TiersListScope.Fournisseurs;
        IsNavDevisActive = p is DevisListViewModel or DevisEditViewModel;
        IsNavBccActive = p is BCVListViewModel or BCVEditViewModel;
        IsNavBlActive = p is BLListViewModel or BLEditViewModel;
        IsNavFacturesActive = p is FactureListViewModel or FactureEditViewModel;
        IsNavAvoirsActive = p is AvoirListViewModel or AvoirEditViewModel;
        IsNavAvoirFournisseurActive = p is AvoirFournisseurListViewModel or AvoirFournisseurEditViewModel;
        IsNavChargesActive = p is ChargeListViewModel or ChargeEditViewModel;
        IsNavBcActive = p is BCListViewModel or BCEditViewModel;
        IsNavBrActive = p is BRListViewModel or BREditViewModel;
        IsNavFacturesFournisseurActive = p is FactureFournisseurListViewModel or FactureFournisseurEditViewModel;
        IsNavStockActive = p is StockMainViewModel;
        IsNavProduitsActive = p is ProduitsViewModel;
        IsNavVendeursActive = p is VendeursViewModel;
        IsNavBonChargeActive = p is BonChargeListViewModel or BonChargeEditViewModel;
        IsNavBonDechargeActive = p is BonDechargeListViewModel or BonDechargeEditViewModel;
        IsNavReportsActive = p is ReportsListViewModel;
        IsNavSettingsActive = p is SettingsViewModel;
    }
}
