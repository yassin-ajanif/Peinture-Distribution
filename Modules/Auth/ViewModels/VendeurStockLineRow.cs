namespace GestionCommerciale.Modules.Auth.ViewModels;

public sealed class VendeurStockLineRow
{
    public required string Reference { get; init; }
    public required string Designation { get; init; }
    public decimal Quantite { get; init; }
    public decimal ValeurVenteTtc { get; init; }
    public string QuantiteLabel => Quantite.ToString("N2");
    public string ValeurVenteTtcLabel { get; init; } = string.Empty;
}
