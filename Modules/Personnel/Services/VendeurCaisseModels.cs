using GestionCommerciale.Modules.Facturation.Models;

namespace GestionCommerciale.Modules.Personnel.Services;

public sealed class VendeurCaisseSummary
{
    public decimal Ventes { get; init; }
    public decimal Encaisse { get; init; }
    public decimal Remis { get; init; }
    public decimal ARemettre => Ventes - Remis;
    public required IReadOnlyList<VendeurRemiseRow> Remises { get; init; }
}

public sealed class VendeurRemiseRow
{
    public int Id { get; init; }
    public string Numero { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public decimal Montant { get; init; }
    public ModePaiement Mode { get; init; }
    public string Note { get; init; } = string.Empty;
    public string DateLabel { get; init; } = string.Empty;
    public string MontantLabel { get; init; } = string.Empty;
    public string ModeLabel { get; init; } = string.Empty;
}
