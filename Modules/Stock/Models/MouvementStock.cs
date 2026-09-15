using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Stock.Models;

public class MouvementStock : BaseEntity
{
    public int ProduitId { get; set; }
    public Produit? Produit { get; set; }

    /// <summary>Source location; null = outside the company.</summary>
    public int? FromLocationId { get; set; }
    public StockLocation? FromLocation { get; set; }

    /// <summary>Destination location; null = left the company.</summary>
    public int? ToLocationId { get; set; }
    public StockLocation? ToLocation { get; set; }

    /// <summary>Always positive amount moved.</summary>
    public decimal Quantite { get; set; }

    public decimal? FromApres { get; set; }
    public decimal? ToApres { get; set; }

    public string OrigineType { get; set; } = string.Empty;
    public int? OrigineId { get; set; }
    public string Note { get; set; } = string.Empty;

    [NotMapped]
    public TypeMouvement Type
    {
        get
        {
            if (FromLocationId is null && ToLocationId is not null)
                return TypeMouvement.Entree;
            if (FromLocationId is not null && ToLocationId is null)
                return TypeMouvement.Sortie;
            if (FromLocationId is not null && ToLocationId is not null)
                return TypeMouvement.Transfert;
            return TypeMouvement.Ajustement;
        }
    }

    /// <summary>
    /// When set (e.g. stock history for a catalogue location), StockApres / SignedQuantite
    /// are relative to that location.
    /// </summary>
    [NotMapped]
    public int? HistoryLocationId { get; set; }

    [NotMapped]
    public decimal StockApres => GetStockApresFor(HistoryLocationId);

    [NotMapped]
    public decimal SignedQuantite => HistoryLocationId is int loc
        ? GetSignedQuantiteFor(loc)
        : Type switch
        {
            TypeMouvement.Sortie => -Math.Abs(Quantite),
            TypeMouvement.Entree => Math.Abs(Quantite),
            TypeMouvement.Transfert => Math.Abs(Quantite),
            TypeMouvement.Ajustement => FromLocationId is not null && ToLocationId is null
                ? -Math.Abs(Quantite)
                : Math.Abs(Quantite),
            _ => Quantite
        };

    public decimal GetStockApresFor(int? locationId)
    {
        if (locationId is int loc)
        {
            if (ToLocationId == loc)
                return ToApres ?? 0m;
            if (FromLocationId == loc)
                return FromApres ?? 0m;
        }

        return FromApres ?? ToApres ?? 0m;
    }

    public decimal GetSignedQuantiteFor(int locationId)
    {
        var qty = Math.Abs(Quantite);
        var fromHere = FromLocationId == locationId;
        var toHere = ToLocationId == locationId;
        if (toHere && !fromHere)
            return qty;
        if (fromHere && !toHere)
            return -qty;
        return 0m;
    }

    [NotMapped]
    public string QuantiteSignedLabel
    {
        get
        {
            var signed = SignedQuantite;
            var formatted = Math.Abs(signed).ToString("N2", CultureInfo.CurrentCulture);
            return signed >= 0 ? $"+{formatted}" : $"-{formatted}";
        }
    }

    [NotMapped]
    public string PartyName { get; set; } = string.Empty;

    [NotMapped]
    public bool PartyIsSupplier { get; set; }

    [NotMapped]
    public bool HasPartyName => !string.IsNullOrWhiteSpace(PartyName);

    [NotMapped]
    public decimal PartyColorSignal => PartyIsSupplier ? 1m : -1m;

    [NotMapped]
    public string DocumentRef => string.IsNullOrWhiteSpace(Note) ? OrigineType : Note;

    [NotMapped]
    public bool CanOpenOrigin => OrigineId is > 0 && OrigineType is "BL" or "BP" or "BR" or "Avoir" or "AvoirFournisseur";

    [NotMapped]
    public string TraceDetail => DocumentRef;

    [NotMapped]
    public string UnitPriceDetail { get; set; } = string.Empty;

    [NotMapped]
    public bool HasUnitPriceDetail => !string.IsNullOrEmpty(UnitPriceDetail);

    [NotMapped]
    public string FromLocationLabel { get; set; } = "—";

    [NotMapped]
    public string ToLocationLabel { get; set; } = "—";

    [NotMapped]
    public string TypeLabel { get; set; } = string.Empty;
}
