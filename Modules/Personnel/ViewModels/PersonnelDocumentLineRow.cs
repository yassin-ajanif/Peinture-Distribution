using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionCommerciale.Modules.Personnel.ViewModels;

public partial class PersonnelDocumentLineRow : ObservableObject
{
    public int ProduitId { get; set; }
    public string Reference { get; set; } = string.Empty;

    [ObservableProperty] private string _designation = string.Empty;
    [ObservableProperty] private decimal _quantite = 1m;
    [ObservableProperty] private decimal _prixUnitaireHT;
    [ObservableProperty] private decimal _remise;
    [ObservableProperty] private decimal _tauxTVA;

    public decimal MontantHt
    {
        get
        {
            var brut = Quantite * PrixUnitaireHT;
            return brut - brut * (Remise / 100m);
        }
    }

    public decimal MontantTtc => MontantHt * (1m + TauxTVA / 100m);

    partial void OnQuantiteChanged(decimal value) => NotifyAmounts();
    partial void OnPrixUnitaireHTChanged(decimal value) => NotifyAmounts();
    partial void OnRemiseChanged(decimal value) => NotifyAmounts();
    partial void OnTauxTVAChanged(decimal value) => NotifyAmounts();

    private void NotifyAmounts()
    {
        OnPropertyChanged(nameof(MontantHt));
        OnPropertyChanged(nameof(MontantTtc));
    }
}
