using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Auth.Services;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;

namespace GestionCommerciale.Modules.Auth.ViewModels;

public partial class VendeursViewModel : BaseViewModel
{
    private readonly IUserService _users;
    private readonly IDialogService _dialog;
    private readonly ILocaleService _locale;

    public VendeursViewModel(IUserService users, IDialogService dialog, ILocaleService locale)
    {
        _users = users;
        _dialog = dialog;
        _locale = locale;
        _locale.CultureApplied += (_, _) => RefreshLabels();
        RefreshLabels();
        Pagination = new PaginationHelper(() => _ = LoadAsync(CancellationToken.None));
    }

    public PaginationHelper Pagination { get; }
    public ObservableCollection<User> Vendeurs { get; } = [];

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

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private User? _selected;
    [ObservableProperty] private bool _isNewDraft;
    [ObservableProperty] private string _ficheNom = string.Empty;
    [ObservableProperty] private string _fichePhone = string.Empty;
    [ObservableProperty] private bool _ficheActif = true;

    public bool FicheEditable => Selected is not null || IsNewDraft;
    public bool CanDelete => Selected is not null && !IsNewDraft;

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
            OnPropertyChanged(nameof(FicheEditable));
            OnPropertyChanged(nameof(CanDelete));
            return;
        }

        IsNewDraft = false;
        FicheNom = value.FullName;
        FichePhone = value.Phone;
        FicheActif = value.Actif;
        OnPropertyChanged(nameof(FicheEditable));
        OnPropertyChanged(nameof(CanDelete));
    }

    partial void OnIsNewDraftChanged(bool value)
    {
        OnPropertyChanged(nameof(FicheEditable));
        OnPropertyChanged(nameof(CanDelete));
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
        OnPropertyChanged(nameof(FicheEditable));
        OnPropertyChanged(nameof(CanDelete));
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
    }
}
