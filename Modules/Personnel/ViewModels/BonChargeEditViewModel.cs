using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Modules.Personnel.Models;
using GestionCommerciale.Modules.Stock;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Modules.Stock.ViewModels;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestionCommerciale.Modules.Personnel.ViewModels;

public partial class BonChargeEditViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly IDialogService _dialog;
    private readonly WorkspaceNavigator _workspace;
    private readonly IServiceProvider _sp;
    private readonly ICurrentUserSession _session;
    private readonly ILocaleService _locale;
    private readonly IStockMovementService _stock;
    private readonly IStockLocationService _locations;
    private bool _suppressAddLinePick;
    private bool _suppressUserSync;
    private bool _suppressDepotSync;

    public BonChargeEditViewModel(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        IDialogService dialog,
        WorkspaceNavigator workspaceNavigator,
        IServiceProvider sp,
        ICurrentUserSession session,
        ILocaleService locale,
        IStockMovementService stock,
        IStockLocationService locations)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _dialog = dialog;
        _workspace = workspaceNavigator;
        _sp = sp;
        _session = session;
        _locale = locale;
        _stock = stock;
        _locations = locations;
        _locale.CultureApplied += (_, _) => RefreshUi();
        Lignes.CollectionChanged += LignesOnCollectionChanged;
        RefreshUi();
    }

    public ObservableCollection<User> Users { get; } = [];
    public ObservableCollection<StockLocationPickItem> StockLocations { get; } = [];
    public ObservableCollection<Produit> Produits { get; } = [];
    public ObservableCollection<PersonnelDocumentLineRow> Lignes { get; } = [];

    [ObservableProperty] private int? _bonChargeId;
    [ObservableProperty] private string _numero = string.Empty;
    [ObservableProperty] private DateTimeOffset _date = new(DateTime.Today);
    [ObservableProperty] private int _assignedToUserId;
    [ObservableProperty] private User? _selectedAssignedUser;
    [ObservableProperty] private int _depotLocationId;
    [ObservableProperty] private StockLocationPickItem? _selectedDepot;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private PersonnelDocumentLineRow? _selectedLine;
    [ObservableProperty] private string _addLineSearchText = string.Empty;
    [ObservableProperty] private object? _addLineCatalogPick;

    [ObservableProperty] private string _btnBack = string.Empty;
    [ObservableProperty] private string _btnSave = string.Empty;
    [ObservableProperty] private string _btnDelete = string.Empty;
    [ObservableProperty] private string _lblAssignedTo = string.Empty;
    [ObservableProperty] private string _lblDepot = string.Empty;
    [ObservableProperty] private string _lblNote = string.Empty;
    [ObservableProperty] private string _btnRemoveLine = string.Empty;
    [ObservableProperty] private string _lblAddProduct = string.Empty;
    [ObservableProperty] private string _wmAddProduct = string.Empty;
    [ObservableProperty] private string _lblDocColDesignation = string.Empty;
    [ObservableProperty] private string _lblDocColQte = string.Empty;
    [ObservableProperty] private string _lblDocColPuHt = string.Empty;
    [ObservableProperty] private string _lblDocColRemise = string.Empty;
    [ObservableProperty] private string _lblDocColTva = string.Empty;
    [ObservableProperty] private string _lblDocColMontantHt = string.Empty;
    [ObservableProperty] private string _lblTotals = string.Empty;

    [ObservableProperty] private decimal _totalHt;
    [ObservableProperty] private decimal _totalTva;
    [ObservableProperty] private decimal _totalTtc;
    [ObservableProperty] private string _totalHtLabel = string.Empty;
    [ObservableProperty] private string _totalTvaLabel = string.Empty;
    [ObservableProperty] private string _totalTtcLabel = string.Empty;
    [ObservableProperty] private string _devise = string.Empty;

    public AutoCompleteFilterPredicate<object?> ProduitAutocompleteFilter => ProductAutoComplete.ItemFilter;
    public bool CanDelete => BonChargeId.HasValue;

    partial void OnBonChargeIdChanged(int? value)
    {
        OnPropertyChanged(nameof(CanDelete));
        DeleteCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedAssignedUserChanged(User? value)
    {
        if (_suppressUserSync || value is null) return;
        if (AssignedToUserId != value.Id)
            AssignedToUserId = value.Id;
    }

    partial void OnAssignedToUserIdChanged(int value)
    {
        if (_suppressUserSync) return;
        if (SelectedAssignedUser?.Id != value)
        {
            _suppressUserSync = true;
            SelectedAssignedUser = Users.FirstOrDefault(u => u.Id == value);
            _suppressUserSync = false;
        }
    }

    partial void OnSelectedDepotChanged(StockLocationPickItem? value)
    {
        if (_suppressDepotSync || value is null) return;
        if (DepotLocationId != value.Id)
            DepotLocationId = value.Id;
    }

    partial void OnDepotLocationIdChanged(int value)
    {
        if (_suppressDepotSync) return;
        if (SelectedDepot?.Id != value)
        {
            _suppressDepotSync = true;
            SelectedDepot = StockLocations.FirstOrDefault(l => l.Id == value)
                ?? StockLocations.FirstOrDefault();
            if (SelectedDepot is not null)
                DepotLocationId = SelectedDepot.Id;
            _suppressDepotSync = false;
        }
    }

    partial void OnAddLineCatalogPickChanged(object? value)
    {
        if (_suppressAddLinePick) return;
        if (value is not Produit p) return;
        _suppressAddLinePick = true;
        var existing = Lignes.FirstOrDefault(l => l.ProduitId == p.Id && p.Id != 0);
        if (existing != null)
        {
            existing.Quantite += 1;
            SelectedLine = existing;
        }
        else
        {
            var row = new PersonnelDocumentLineRow();
            ApplyCatalogProduct(row, p);
            row.Quantite = 1;
            Lignes.Add(row);
            SelectedLine = row;
        }
        DocumentLineSearchHelper.ClearAfterCatalogPick(() =>
        {
            _suppressAddLinePick = true;
            AddLineCatalogPick = null;
            AddLineSearchText = string.Empty;
            _suppressAddLinePick = false;
        });
    }

    private void RefreshUi()
    {
        BtnBack = _locale.T("Btn_Back");
        BtnSave = _locale.T("Btn_Save");
        BtnDelete = _locale.T("Btn_Delete");
        LblAssignedTo = _locale.T("Lbl_AssignedTo");
        LblDepot = _locale.T("Lbl_Depot");
        LblNote = _locale.T("Lbl_Note");
        BtnRemoveLine = _locale.T("Btn_RemoveLine");
        LblAddProduct = _locale.T("Devis_LblAddProduct");
        WmAddProduct = _locale.T("Devis_WmSearchProduct");
        LblDocColDesignation = _locale.T("DocLine_ColDesignation");
        LblDocColQte = _locale.T("DocLine_ColQte");
        LblDocColPuHt = _locale.T("DocLine_ColPuHt");
        LblDocColRemise = _locale.T("DocLine_ColRemise");
        LblDocColTva = _locale.T("DocLine_ColTva");
        LblDocColMontantHt = _locale.T("DocLine_ColMontantHt");
        LblTotals = _locale.T("Lbl_Totals");
        UpdateTotalLabels(TotalHt, TotalTva, TotalTtc);
    }

    private void LignesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (PersonnelDocumentLineRow row in e.NewItems)
                row.PropertyChanged += LineOnPropertyChanged;
        if (e.OldItems != null)
            foreach (PersonnelDocumentLineRow row in e.OldItems)
                row.PropertyChanged -= LineOnPropertyChanged;
        RefreshTotals();
    }

    private void LineOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshTotals();

    private void RefreshTotals()
    {
        var ht = Lignes.Sum(l => l.MontantHt);
        var tva = Lignes.Sum(l => l.MontantHt * (l.TauxTVA / 100m));
        var ttc = ht + tva;
        TotalHt = ht;
        TotalTva = tva;
        TotalTtc = ttc;
        UpdateTotalLabels(ht, tva, ttc);
    }

    private void UpdateTotalLabels(decimal ht, decimal tva, decimal ttc)
    {
        TotalHtLabel = _locale.Tf("Doc_FmtHt", ht, Devise).TrimEnd();
        TotalTvaLabel = _locale.Tf("Doc_FmtTva", tva, Devise).TrimEnd();
        TotalTtcLabel = _locale.Tf("Doc_FmtTtc", ttc, Devise).TrimEnd();
    }

    public void Load(int? id) => _ = LoadAsync(id, CancellationToken.None);

    public async Task LoadAsync(int? id, CancellationToken cancellationToken = default)
    {
        BonChargeId = id;
        Lignes.Clear();

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var cfg = await db.AppSettings.AsNoTracking().FirstAsync(cancellationToken);
        Devise = CurrencyHelper.FromSettings(cfg);

        var users = await db.Users.AsNoTracking()
            .Where(u => u.Actif && u.UserType == UserType.Vendeur)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
        Users.Clear();
        foreach (var u in users) Users.Add(u);

        var produits = await db.Produits.AsNoTracking().Where(p => p.Actif)
            .SelectForListWithoutImageData().ToListAsync(cancellationToken);
        Produits.Clear();
        foreach (var p in produits) Produits.Add(p);

        var locations = await _locations.GetActiveLocationsAsync(db, cancellationToken);
        StockLocations.Clear();
        foreach (var loc in locations.Where(l => !l.IsVirtual))
        {
            StockLocations.Add(new StockLocationPickItem
            {
                Id = loc.Id,
                Nom = loc.Nom,
                IsVirtual = false,
                Label = loc.Nom
            });
        }

        if (id == null)
        {
            var defaultDepot = await _locations.GetOrCreateDefaultDepotAsync(db, cancellationToken);
            Numero = "(nouveau)";
            Date = new DateTimeOffset(DateTime.Today);
            Note = string.Empty;
            AssignedToUserId = Users.FirstOrDefault()?.Id ?? 0;
            _suppressUserSync = true;
            SelectedAssignedUser = Users.FirstOrDefault();
            _suppressUserSync = false;
            SelectDepot(defaultDepot.Id);
            Title = _locale.T("BCH_NewTitle");
            RefreshTotals();
            return;
        }

        var b = await db.BonsCharge.Include(x => x.Lignes).FirstAsync(x => x.Id == id, cancellationToken);
        Numero = b.Numero;
        AssignedToUserId = b.AssignedToUserId;
        _suppressUserSync = true;
        SelectedAssignedUser = Users.FirstOrDefault(u => u.Id == b.AssignedToUserId);
        _suppressUserSync = false;
        SelectDepot(b.DepotLocationId);
        Date = new DateTimeOffset(b.Date);
        Note = b.Note;
        foreach (var l in b.Lignes)
        {
            var prod = Produits.FirstOrDefault(p => p.Id == l.ProduitId);
            Lignes.Add(new PersonnelDocumentLineRow
            {
                ProduitId = l.ProduitId,
                Reference = prod?.Reference ?? string.Empty,
                Designation = l.Designation,
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                Remise = l.Remise,
                TauxTVA = l.TauxTVA
            });
        }

        Title = _locale.Tf("BCH_EditTitle", Numero);
        RefreshTotals();
    }

    private void SelectDepot(int locationId)
    {
        _suppressDepotSync = true;
        DepotLocationId = locationId;
        SelectedDepot = StockLocations.FirstOrDefault(l => l.Id == locationId)
            ?? StockLocations.FirstOrDefault();
        if (SelectedDepot is not null)
            DepotLocationId = SelectedDepot.Id;
        _suppressDepotSync = false;
    }

    private static void ApplyCatalogProduct(PersonnelDocumentLineRow row, Produit p)
    {
        row.ProduitId = p.Id;
        row.Reference = p.Reference;
        row.Designation = p.Designation;
        row.PrixUnitaireHT = p.PrixVenteHT;
        row.TauxTVA = p.TauxTVA;
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedLine == null) return;
        Lignes.Remove(SelectedLine);
        SelectedLine = null;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (AssignedToUserId == 0)
        {
            await _dialog.ShowErrorAsync(_locale.T("BCH_Title"), _locale.T("BCH_ErrAssigned"), cancellationToken);
            return;
        }

        if (DepotLocationId == 0 || SelectedDepot is null)
        {
            await _dialog.ShowErrorAsync(_locale.T("BCH_Title"), _locale.T("BCH_ErrDepot"), cancellationToken);
            return;
        }

        if (!Lignes.Any(l => l.ProduitId > 0 && l.Quantite > 0))
        {
            await _dialog.ShowErrorAsync(_locale.T("BCH_Title"), _locale.T("BCH_ErrLines"), cancellationToken);
            return;
        }

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            BonCharge entity;
            if (BonChargeId == null)
            {
                var num = await _numbers.NextBonChargeAsync(cancellationToken);
                entity = new BonCharge
                {
                    Numero = num,
                    AssignedToUserId = AssignedToUserId,
                    DepotLocationId = DepotLocationId,
                    Date = Date.DateTime,
                    Note = Note
                };
                foreach (var l in Lignes.Where(x => x.ProduitId > 0 && x.Quantite > 0))
                {
                    entity.Lignes.Add(new BonChargeLigne
                    {
                        ProduitId = l.ProduitId,
                        Designation = l.Designation,
                        Quantite = l.Quantite,
                        PrixUnitaireHT = l.PrixUnitaireHT,
                        Remise = l.Remise,
                        TauxTVA = l.TauxTVA
                    });
                }
                db.BonsCharge.Add(entity);
                await db.SaveChangesAsync(cancellationToken);
                BonChargeId = entity.Id;
            }
            else
            {
                entity = await db.BonsCharge.Include(b => b.Lignes)
                    .FirstAsync(b => b.Id == BonChargeId, cancellationToken);
                entity.AssignedToUserId = AssignedToUserId;
                entity.DepotLocationId = DepotLocationId;
                entity.Date = Date.DateTime;
                entity.Note = Note;
                db.BonChargeLignes.RemoveRange(entity.Lignes);
                entity.Lignes.Clear();
                foreach (var l in Lignes.Where(x => x.ProduitId > 0 && x.Quantite > 0))
                {
                    entity.Lignes.Add(new BonChargeLigne
                    {
                        ProduitId = l.ProduitId,
                        Designation = l.Designation,
                        Quantite = l.Quantite,
                        PrixUnitaireHT = l.PrixUnitaireHT,
                        Remise = l.Remise,
                        TauxTVA = l.TauxTVA
                    });
                }
                await db.SaveChangesAsync(cancellationToken);
            }

            var user = await db.Users.FirstAsync(u => u.Id == AssignedToUserId, cancellationToken);
            var virtualLoc = await _locations.GetOrCreateVirtualForUserAsync(db, user, cancellationToken);
            var stockLines = Lignes
                .Where(l => l.ProduitId > 0 && l.Quantite > 0)
                .Select(l => (l.ProduitId, l.Quantite));
            await _stock.ResyncBonChargeStockAsync(
                db, entity.Id, entity.Numero, DepotLocationId, virtualLoc.Id,
                stockLines, _session.UserId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            Numero = entity.Numero;
            await LoadAsync(BonChargeId, cancellationToken);
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("BCH_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (BonChargeId is not { } id) return;
        if (!await _dialog.ConfirmAsync(_locale.T("BCH_Title"), _locale.T("BCH_ConfirmDelete"), cancellationToken))
            return;

        IsBusy = true;
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var entity = await db.BonsCharge.Include(b => b.Lignes)
                .FirstAsync(b => b.Id == id, cancellationToken);
            var user = await db.Users.FirstAsync(u => u.Id == entity.AssignedToUserId, cancellationToken);
            var virtualLoc = await _locations.GetOrCreateVirtualForUserAsync(db, user, cancellationToken);
            await _stock.ResyncBonChargeStockAsync(
                db, entity.Id, entity.Numero, entity.DepotLocationId, virtualLoc.Id,
                [], _session.UserId, cancellationToken);
            db.BonsCharge.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
            Back();
        }
        catch (Exception ex)
        {
            await _dialog.ShowErrorAsync(_locale.T("BCH_Title"), ex.Message, cancellationToken);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        var list = _sp.GetRequiredService<BonChargeListViewModel>();
        _workspace.Open(list);
        list.LoadCommand.Execute(null);
    }
}
