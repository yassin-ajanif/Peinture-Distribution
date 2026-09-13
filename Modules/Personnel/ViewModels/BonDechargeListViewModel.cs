using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Shared.Services;
using GestionCommerciale.Shared.ViewModels;

namespace GestionCommerciale.Modules.Personnel.ViewModels;

public partial class BonDechargeListViewModel : BaseViewModel
{
    private readonly ILocaleService _locale;

    public BonDechargeListViewModel(ILocaleService locale)
    {
        _locale = locale;
        _locale.CultureApplied += (_, _) => RefreshUi();
        RefreshUi();
    }

    [ObservableProperty] private string _placeholder = string.Empty;

    private void RefreshUi()
    {
        Title = _locale.T("Nav_BonDecharge");
        Placeholder = _locale.T("Lbl_ModuleComingSoon");
    }
}
