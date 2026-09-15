using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Stock.Services;

public interface IStockMovementService
{
    Task ApplyMovementAsync(
        AppDbContext db,
        int produitId,
        TypeMouvement type,
        decimal quantite,
        string origineType,
        int? origineId,
        string? note,
        int? createdByUserId,
        CancellationToken cancellationToken = default,
        int? stockLocationId = null);

    Task ResyncBonLivraisonStockAsync(
        AppDbContext db,
        int bonLivraisonId,
        string noteDetail,
        IEnumerable<(int ProduitId, decimal QuantiteLivree)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task SyncBonReceptionStockAsync(
        AppDbContext db,
        int bonReceptionId,
        string noteDetail,
        IEnumerable<(int ProduitId, decimal QuantiteRecue, decimal PrixUnitaireHT)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task SyncAvoirStockAsync(
        AppDbContext db,
        int avoirId,
        string noteDetail,
        bool retourMarchandise,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    Task SyncAvoirFournisseurStockAsync(
        AppDbContext db,
        int avoirFournisseurId,
        string noteDetail,
        bool retourMarchandise,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Depot → assigned user's virtual stock (Bon de charge).</summary>
    Task ResyncBonChargeStockAsync(
        AppDbContext db,
        int bonChargeId,
        string noteDetail,
        int depotLocationId,
        int virtualLocationId,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Assigned user's virtual stock → depot (Bon de décharge).</summary>
    Task ResyncBonDechargeStockAsync(
        AppDbContext db,
        int bonDechargeId,
        string noteDetail,
        int depotLocationId,
        int virtualLocationId,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Products where additional outbound from <paramref name="fromLocationId"/> would exceed available stock.
    /// Accounts for quantities already applied by the same document (<paramref name="origineType"/> / <paramref name="origineId"/>).
    /// </summary>
    Task<IReadOnlyList<StockShortageItem>> GetOutboundShortagesAsync(
        AppDbContext db,
        int fromLocationId,
        IEnumerable<(int ProduitId, decimal Quantite)> desiredOutboundLines,
        string? origineType = null,
        int? origineId = null,
        CancellationToken cancellationToken = default);
}
