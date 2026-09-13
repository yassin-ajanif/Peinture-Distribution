using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Stock.Services;

public sealed class StockMovementService : IStockMovementService
{
    public const string OrigineTypeBonLivraison = "BL";
    public const string OrigineTypeBonPreparation = "BP";
    public const string OrigineTypeBonReception = "BR";
    public const string OrigineTypeAvoir = "Avoir";
    public const string OrigineTypeAvoirFournisseur = "AvoirFournisseur";
    public const string OrigineTypeImport = "Import";
    public const string OrigineTypeBonCharge = "BCH";
    public const string OrigineTypeBonDecharge = "BDH";

    private readonly ILocaleService _locale;
    private readonly IStockLocationService _locations;

    public StockMovementService(ILocaleService locale, IStockLocationService locations)
    {
        _locale = locale;
        _locations = locations;
    }

    public async Task ApplyMovementAsync(
        AppDbContext db,
        int produitId,
        TypeMouvement type,
        decimal quantite,
        string origineType,
        int? origineId,
        string? note,
        int? createdByUserId,
        CancellationToken cancellationToken = default,
        int? stockLocationId = null)
    {
        var locationId = stockLocationId
            ?? (await _locations.GetOrCreateDefaultDepotAsync(db, cancellationToken)).Id;
        await ApplyMovementAtLocationAsync(
            db, produitId, type, quantite, locationId, origineType, origineId, note, createdByUserId, cancellationToken);
    }

    public Task ResyncBonLivraisonStockAsync(
        AppDbContext db,
        int bonLivraisonId,
        string noteDetail,
        IEnumerable<(int ProduitId, decimal QuantiteLivree)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var desired = lines
            .Where(l => l.ProduitId > 0 && l.QuantiteLivree > 0)
            .GroupBy(l => l.ProduitId)
            .ToDictionary(g => g.Key, g => -g.Sum(l => l.QuantiteLivree));

        return SyncDocumentStockAsync(
            db,
            OrigineTypeBonLivraison,
            bonLivraisonId,
            noteDetail,
            desired,
            stockLocationId: null,
            createdByUserId,
            useModificationNoteOnEdit: true,
            onPositiveEntreeDelta: null,
            cancellationToken);
    }

    public async Task ResyncBonPreparationStockAsync(
        AppDbContext db,
        int bonPreparationId,
        string noteDetail,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int stockLocationId,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var location = await db.StockLocations.FirstOrDefaultAsync(l => l.Id == stockLocationId, cancellationToken)
            ?? throw new InvalidOperationException("Emplacement de stock introuvable.");
        if (!location.Actif)
            throw new InvalidOperationException("Emplacement de stock inactif.");

        await NeutralizeMovementsNotOnLocationAsync(
            db, OrigineTypeBonPreparation, bonPreparationId, stockLocationId, createdByUserId, cancellationToken);

        var desired = lines
            .Where(l => l.ProduitId > 0 && l.Quantite > 0)
            .GroupBy(l => l.ProduitId)
            .ToDictionary(g => g.Key, g => -g.Sum(l => l.Quantite));

        await SyncDocumentStockAsync(
            db,
            OrigineTypeBonPreparation,
            bonPreparationId,
            noteDetail,
            desired,
            stockLocationId,
            createdByUserId,
            useModificationNoteOnEdit: true,
            onPositiveEntreeDelta: null,
            cancellationToken);
    }

    public Task SyncBonReceptionStockAsync(
        AppDbContext db,
        int bonReceptionId,
        string noteDetail,
        IEnumerable<(int ProduitId, decimal QuantiteRecue, decimal PrixUnitaireHT)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var lineList = lines.Where(l => l.ProduitId > 0 && l.QuantiteRecue > 0).ToList();
        var desired = lineList
            .GroupBy(l => l.ProduitId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.QuantiteRecue));

        var prixByProduit = lineList
            .GroupBy(l => l.ProduitId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var totalQty = g.Sum(l => l.QuantiteRecue);
                    var weighted = g.Sum(l => l.QuantiteRecue * l.PrixUnitaireHT);
                    return totalQty > 0 ? weighted / totalQty : 0m;
                });

        return SyncDocumentStockAsync(
            db,
            OrigineTypeBonReception,
            bonReceptionId,
            noteDetail,
            desired,
            stockLocationId: null,
            createdByUserId,
            useModificationNoteOnEdit: true,
            onPositiveEntreeDelta: async (produitId, entreeDelta, ct) =>
            {
                if (!prixByProduit.TryGetValue(produitId, out var newPrice)) return;
                var produit = await db.Produits.FirstAsync(p => p.Id == produitId, ct);
                var depot = await _locations.GetOrCreateDefaultDepotAsync(db, ct);
                var balanceAfter = await StockBalanceQueries.GetBalanceAsync(db, produitId, depot.Id, ct);
                var oldQty = balanceAfter - entreeDelta;
                var oldPrice = produit.PrixAchatHT;
                var totalQty = oldQty + entreeDelta;
                if (totalQty > 0)
                    produit.PrixAchatHT = (oldQty * oldPrice + entreeDelta * newPrice) / totalQty;
            },
            cancellationToken);
    }

    public Task SyncAvoirStockAsync(
        AppDbContext db,
        int avoirId,
        string noteDetail,
        bool retourMarchandise,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var desired = retourMarchandise
            ? lines
                .Where(l => l.ProduitId > 0 && l.Quantite > 0)
                .GroupBy(l => l.ProduitId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantite))
            : [];

        return SyncDocumentStockAsync(
            db,
            OrigineTypeAvoir,
            avoirId,
            noteDetail,
            desired,
            stockLocationId: null,
            createdByUserId,
            useModificationNoteOnEdit: true,
            onPositiveEntreeDelta: null,
            cancellationToken);
    }

    public Task SyncAvoirFournisseurStockAsync(
        AppDbContext db,
        int avoirFournisseurId,
        string noteDetail,
        bool retourMarchandise,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var desired = retourMarchandise
            ? lines
                .Where(l => l.ProduitId > 0 && l.Quantite > 0)
                .GroupBy(l => l.ProduitId)
                .ToDictionary(g => g.Key, g => -g.Sum(l => l.Quantite))
            : [];

        return SyncDocumentStockAsync(
            db,
            OrigineTypeAvoirFournisseur,
            avoirFournisseurId,
            noteDetail,
            desired,
            stockLocationId: null,
            createdByUserId,
            useModificationNoteOnEdit: true,
            onPositiveEntreeDelta: null,
            cancellationToken);
    }

    public Task ResyncBonChargeStockAsync(
        AppDbContext db,
        int bonChargeId,
        string noteDetail,
        int depotLocationId,
        int virtualLocationId,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
        => SyncTransferDocumentStockAsync(
            db,
            OrigineTypeBonCharge,
            bonChargeId,
            noteDetail,
            fromLocationId: depotLocationId,
            toLocationId: virtualLocationId,
            lines,
            createdByUserId,
            cancellationToken);

    public Task ResyncBonDechargeStockAsync(
        AppDbContext db,
        int bonDechargeId,
        string noteDetail,
        int depotLocationId,
        int virtualLocationId,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken = default)
        => SyncTransferDocumentStockAsync(
            db,
            OrigineTypeBonDecharge,
            bonDechargeId,
            noteDetail,
            fromLocationId: virtualLocationId,
            toLocationId: depotLocationId,
            lines,
            createdByUserId,
            cancellationToken);

    private async Task SyncTransferDocumentStockAsync(
        AppDbContext db,
        string origineType,
        int origineId,
        string noteDetail,
        int fromLocationId,
        int toLocationId,
        IEnumerable<(int ProduitId, decimal Quantite)> lines,
        int? createdByUserId,
        CancellationToken cancellationToken)
    {
        if (fromLocationId <= 0 || toLocationId <= 0)
            throw new ArgumentException("Transfer locations are required.");
        if (fromLocationId == toLocationId)
            throw new ArgumentException("From and To locations must differ.");

        var fromOk = await db.StockLocations.AsNoTracking()
            .AnyAsync(l => l.Id == fromLocationId && l.Actif, cancellationToken);
        var toOk = await db.StockLocations.AsNoTracking()
            .AnyAsync(l => l.Id == toLocationId && l.Actif, cancellationToken);
        if (!fromOk || !toOk)
            throw new InvalidOperationException(_locale.T("Stock_ErrLocationInactive"));

        var desired = lines
            .Where(l => l.ProduitId > 0 && l.Quantite > 0)
            .GroupBy(l => l.ProduitId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantite));

        var movements = await db.MouvementsStock
            .Where(m => m.OrigineType == origineType && m.OrigineId == origineId)
            .ToListAsync(cancellationToken);

        var documentHasPriorMovements = movements.Count > 0;

        var currentByProduit = movements
            .GroupBy(m => m.ProduitId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(m => TransferNetQty(m, fromLocationId, toLocationId)));

        var produitIds = currentByProduit.Keys.Union(desired.Keys).ToList();
        foreach (var produitId in produitIds)
        {
            currentByProduit.TryGetValue(produitId, out var current);
            desired.TryGetValue(produitId, out var want);
            var delta = want - current;
            if (delta == 0) continue;

            var isAnnulation = want == 0 && current != 0;
            var isModification = !isAnnulation && documentHasPriorMovements;
            var note = isAnnulation
                ? _locale.Tf("Stock_AnnulationNote", noteDetail)
                : isModification
                    ? _locale.Tf("Stock_ModificationNote", noteDetail)
                    : noteDetail;

            if (delta > 0)
            {
                await ApplyLocationMovementAsync(
                    db, produitId, fromLocationId, toLocationId, delta,
                    origineType, origineId, note, createdByUserId, cancellationToken);
            }
            else
            {
                await ApplyLocationMovementAsync(
                    db, produitId, toLocationId, fromLocationId, -delta,
                    origineType, origineId, note, createdByUserId, cancellationToken);
            }
        }
    }

    private static decimal TransferNetQty(MouvementStock m, int fromLocationId, int toLocationId)
    {
        if (m.FromLocationId == fromLocationId && m.ToLocationId == toLocationId)
            return Math.Abs(m.Quantite);
        if (m.FromLocationId == toLocationId && m.ToLocationId == fromLocationId)
            return -Math.Abs(m.Quantite);
        return 0m;
    }

    private async Task SyncDocumentStockAsync(
        AppDbContext db,
        string origineType,
        int origineId,
        string noteDetail,
        IReadOnlyDictionary<int, decimal> desiredSignedByProduit,
        int? stockLocationId,
        int? createdByUserId,
        bool useModificationNoteOnEdit,
        Func<int, decimal, CancellationToken, Task>? onPositiveEntreeDelta,
        CancellationToken cancellationToken)
    {
        var locationId = stockLocationId
            ?? (await _locations.GetOrCreateDefaultDepotAsync(db, cancellationToken)).Id;

        var movements = await db.MouvementsStock
            .Where(m => m.OrigineType == origineType && m.OrigineId == origineId)
            .ToListAsync(cancellationToken);

        var documentHasPriorMovements = movements.Count > 0;

        var currentSignedByProduit = movements
            .Where(m => TouchesLocation(m, locationId))
            .GroupBy(m => m.ProduitId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.SignedQuantite));

        var produitIds = currentSignedByProduit.Keys
            .Union(desiredSignedByProduit.Keys)
            .ToList();

        foreach (var produitId in produitIds)
        {
            currentSignedByProduit.TryGetValue(produitId, out var current);
            desiredSignedByProduit.TryGetValue(produitId, out var desired);
            var delta = desired - current;
            if (delta == 0) continue;

            var isAnnulation = desired == 0 && current != 0;
            var isModification = useModificationNoteOnEdit && !isAnnulation && documentHasPriorMovements;
            var note = isAnnulation
                ? _locale.Tf("Stock_AnnulationNote", noteDetail)
                : isModification
                    ? _locale.Tf("Stock_ModificationNote", noteDetail)
                    : noteDetail;

            if (delta > 0)
            {
                await ApplyMovementAtLocationAsync(
                    db,
                    produitId,
                    TypeMouvement.Entree,
                    delta,
                    locationId,
                    origineType,
                    origineId,
                    note,
                    createdByUserId,
                    cancellationToken);

                if (onPositiveEntreeDelta != null)
                    await onPositiveEntreeDelta(produitId, delta, cancellationToken);
            }
            else
            {
                await ApplyMovementAtLocationAsync(
                    db,
                    produitId,
                    TypeMouvement.Sortie,
                    -delta,
                    locationId,
                    origineType,
                    origineId,
                    note,
                    createdByUserId,
                    cancellationToken);
            }
        }
    }

    /// <summary>
    /// When BP stock location changes, reverse prior movements that hit another location
    /// so the new location sync starts from a clean net for this document.
    /// </summary>
    private async Task NeutralizeMovementsNotOnLocationAsync(
        AppDbContext db,
        string origineType,
        int origineId,
        int stockLocationId,
        int? createdByUserId,
        CancellationToken cancellationToken)
    {
        var movements = await db.MouvementsStock
            .Where(m => m.OrigineType == origineType && m.OrigineId == origineId)
            .ToListAsync(cancellationToken);

        var foreign = movements.Where(m => !TouchesLocation(m, stockLocationId)).ToList();
        if (foreign.Count == 0)
            return;

        var note = _locale.Tf("Stock_AnnulationNote", origineType);

        foreach (var group in foreign.GroupBy(m => m.ProduitId))
        {
            var net = group.Sum(m => m.SignedQuantite);
            if (net == 0) continue;

            // Reverse net impact: if net was -5 (sortie), apply +5 entree on the foreign location(s).
            // Prefer the From/To location that carried the net.
            var sample = group.OrderByDescending(m => m.Id).First();
            var foreignLocationId = sample.FromLocationId ?? sample.ToLocationId;
            if (foreignLocationId is null) continue;

            if (net < 0)
            {
                await ApplyMovementAtLocationAsync(
                    db, group.Key, TypeMouvement.Entree, -net, foreignLocationId.Value,
                    origineType, origineId, note, createdByUserId, cancellationToken);
            }
            else
            {
                await ApplyMovementAtLocationAsync(
                    db, group.Key, TypeMouvement.Sortie, net, foreignLocationId.Value,
                    origineType, origineId, note, createdByUserId, cancellationToken);
            }
        }
    }

    private static bool TouchesLocation(MouvementStock m, int locationId) =>
        m.FromLocationId == locationId || m.ToLocationId == locationId;

    private async Task ApplyMovementAtLocationAsync(
        AppDbContext db,
        int produitId,
        TypeMouvement type,
        decimal quantite,
        int locationId,
        string origineType,
        int? origineId,
        string? note,
        int? createdByUserId,
        CancellationToken cancellationToken)
    {
        _ = await db.Produits.FirstAsync(p => p.Id == produitId, cancellationToken);

        int? fromId;
        int? toId;
        var absQty = Math.Abs(quantite);

        switch (type)
        {
            case TypeMouvement.Entree:
                fromId = null;
                toId = locationId;
                break;
            case TypeMouvement.Sortie:
                fromId = locationId;
                toId = null;
                break;
            case TypeMouvement.Ajustement when quantite >= 0:
                fromId = null;
                toId = locationId;
                break;
            case TypeMouvement.Ajustement:
                fromId = locationId;
                toId = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        await ApplyLocationMovementAsync(
            db,
            produitId,
            fromId,
            toId,
            absQty,
            origineType,
            origineId,
            note,
            createdByUserId,
            cancellationToken);
    }

    private async Task ApplyLocationMovementAsync(
        AppDbContext db,
        int produitId,
        int? fromId,
        int? toId,
        decimal quantite,
        string origineType,
        int? origineId,
        string? note,
        int? createdByUserId,
        CancellationToken cancellationToken)
    {
        if (quantite <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantite));
        if (fromId is null && toId is null)
            throw new ArgumentException("From and To cannot both be null.");

        decimal? fromApres = null, toApres = null;

        if (fromId is int fid)
        {
            var fromAvant = await StockBalanceQueries.GetBalanceAsync(db, produitId, fid, cancellationToken);
            fromApres = fromAvant - quantite;
        }

        if (toId is int tid)
        {
            var toAvant = await StockBalanceQueries.GetBalanceAsync(db, produitId, tid, cancellationToken);
            toApres = toAvant + quantite;
        }

        db.MouvementsStock.Add(new MouvementStock
        {
            ProduitId = produitId,
            FromLocationId = fromId,
            ToLocationId = toId,
            Quantite = quantite,
            FromApres = fromApres,
            ToApres = toApres,
            OrigineType = origineType,
            OrigineId = origineId,
            Note = note ?? string.Empty,
            CreatedByUserId = createdByUserId
        });
    }
}
