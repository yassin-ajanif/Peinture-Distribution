using GestionCommerciale.Modules.Facturation.Models;

namespace GestionCommerciale.Modules.Personnel.Services;

public sealed class VendeurCaisseSummary
{
    public decimal Ventes { get; init; }
    public decimal Encaisse { get; init; }
    public decimal Remis { get; init; }
    public decimal ARemettre => Ventes - Remis;
    public required IReadOnlyList<VendeurVenteBlRow> VenteBls { get; init; }
    public required IReadOnlyList<VendeurEncaisseRow> Encaissements { get; init; }
    public required IReadOnlyList<VendeurRemiseRow> Remises { get; init; }
}

public sealed class VendeurVenteBlRow
{
    public int Id { get; init; }
    public string Numero { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string ClientNom { get; init; } = string.Empty;
    public decimal Montant { get; init; }
    public string DateLabel { get; init; } = string.Empty;
    public string MontantLabel { get; init; } = string.Empty;
}

public sealed class VendeurEncaisseRow
{
    public int PaiementId { get; init; }
    public int BonLivraisonId { get; init; }
    public string BlNumero { get; init; } = string.Empty;
    /// <summary>BL date — used for caisse period filters (same basis as Ventes).</summary>
    public DateTime BlDate { get; init; }
    public DateTime Date { get; init; }
    public ModePaiement Mode { get; init; }
    public decimal Montant { get; init; }
    public string DateLabel { get; init; } = string.Empty;
    public string MontantLabel { get; init; } = string.Empty;
    public string ModeLabel { get; init; } = string.Empty;
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
