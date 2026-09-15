using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Stock;

public static class MouvementStockEnricher
{
    public static async Task EnrichMovementDetailsAsync(
        AppDbContext db,
        IReadOnlyList<MouvementStock> movements,
        string prixHtLabel,
        CancellationToken cancellationToken)
    {
        if (movements.Count == 0) return;

        var blIds = movements
            .Where(m => m.OrigineType == StockMovementService.OrigineTypeBonLivraison && m.OrigineId.HasValue)
            .Select(m => m.OrigineId!.Value)
            .Distinct()
            .ToList();
        var brIds = movements
            .Where(m => m.OrigineType == StockMovementService.OrigineTypeBonReception && m.OrigineId.HasValue)
            .Select(m => m.OrigineId!.Value)
            .Distinct()
            .ToList();
        var avoirIds = movements
            .Where(m => m.OrigineType == StockMovementService.OrigineTypeAvoir && m.OrigineId.HasValue)
            .Select(m => m.OrigineId!.Value)
            .Distinct()
            .ToList();
        var avoirFournisseurIds = movements
            .Where(m => m.OrigineType == StockMovementService.OrigineTypeAvoirFournisseur && m.OrigineId.HasValue)
            .Select(m => m.OrigineId!.Value)
            .Distinct()
            .ToList();

        var blParties = blIds.Count == 0
            ? []
            : await db.BonsLivraison.AsNoTracking()
                .Where(b => blIds.Contains(b.Id))
                .Select(b => new { b.Id, b.ClientId })
                .ToListAsync(cancellationToken);

        var brParties = brIds.Count == 0
            ? []
            : await db.BonsReception.AsNoTracking()
                .Where(b => brIds.Contains(b.Id))
                .Select(b => new { b.Id, b.FournisseurId })
                .ToListAsync(cancellationToken);

        var avoirParties = avoirIds.Count == 0
            ? []
            : await db.Avoirs.AsNoTracking()
                .Where(a => avoirIds.Contains(a.Id))
                .Select(a => new { a.Id, a.ClientId })
                .ToListAsync(cancellationToken);

        var avoirFournisseurParties = avoirFournisseurIds.Count == 0
            ? []
            : await db.AvoirsFournisseurs.AsNoTracking()
                .Where(a => avoirFournisseurIds.Contains(a.Id))
                .Select(a => new { a.Id, a.FournisseurId })
                .ToListAsync(cancellationToken);

        var tierIds = blParties.Select(x => x.ClientId)
            .Concat(brParties.Select(x => x.FournisseurId))
            .Concat(avoirParties.Select(x => x.ClientId))
            .Concat(avoirFournisseurParties.Select(x => x.FournisseurId))
            .Distinct()
            .ToList();

        var tierNames = tierIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.Tiers.AsNoTracking()
                .Where(t => tierIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Nom, cancellationToken);

        var blMap = blParties.ToDictionary(x => x.Id, x => tierNames.GetValueOrDefault(x.ClientId, string.Empty));
        var brMap = brParties.ToDictionary(x => x.Id, x => tierNames.GetValueOrDefault(x.FournisseurId, string.Empty));
        var avoirMap = avoirParties.ToDictionary(x => x.Id, x => tierNames.GetValueOrDefault(x.ClientId, string.Empty));
        var avoirFournisseurMap = avoirFournisseurParties.ToDictionary(x => x.Id, x => tierNames.GetValueOrDefault(x.FournisseurId, string.Empty));

        var blPriceMap = blIds.Count == 0
            ? new Dictionary<(int, int), decimal>()
            : (await db.BonLivraisonLignes.AsNoTracking()
                .Where(l => blIds.Contains(l.BLId))
                .Select(l => new { l.BLId, l.ProduitId, l.PrixUnitaireHT })
                .ToListAsync(cancellationToken))
                .GroupBy(l => (l.BLId, l.ProduitId))
                .ToDictionary(g => g.Key, g => g.Last().PrixUnitaireHT);

        var brPriceMap = brIds.Count == 0
            ? new Dictionary<(int, int), decimal>()
            : (await db.BonReceptionLignes.AsNoTracking()
                .Where(l => brIds.Contains(l.BRId))
                .Select(l => new { l.BRId, l.ProduitId, l.PrixUnitaireHT })
                .ToListAsync(cancellationToken))
                .GroupBy(l => (l.BRId, l.ProduitId))
                .ToDictionary(g => g.Key, g => g.Last().PrixUnitaireHT);

        var avoirPriceMap = avoirIds.Count == 0
            ? new Dictionary<(int, int), decimal>()
            : (await db.AvoirLignes.AsNoTracking()
                .Where(l => avoirIds.Contains(l.AvoirId))
                .Select(l => new { l.AvoirId, l.ProduitId, l.PrixUnitaireHT })
                .ToListAsync(cancellationToken))
                .GroupBy(l => (l.AvoirId, l.ProduitId))
                .ToDictionary(g => g.Key, g => g.Last().PrixUnitaireHT);

        var avoirFournisseurPriceMap = avoirFournisseurIds.Count == 0
            ? new Dictionary<(int, int), decimal>()
            : (await db.AvoirFournisseurLignes.AsNoTracking()
                .Where(l => avoirFournisseurIds.Contains(l.AvoirFournisseurId))
                .Select(l => new { l.AvoirFournisseurId, l.ProduitId, l.PrixUnitaireHT })
                .ToListAsync(cancellationToken))
                .GroupBy(l => (l.AvoirFournisseurId, l.ProduitId))
                .ToDictionary(g => g.Key, g => g.Last().PrixUnitaireHT);

        foreach (var m in movements)
        {
            m.PartyName = m.OrigineType switch
            {
                StockMovementService.OrigineTypeBonLivraison when m.OrigineId is int blId => blMap.GetValueOrDefault(blId, string.Empty),
                StockMovementService.OrigineTypeBonReception when m.OrigineId is int brId => brMap.GetValueOrDefault(brId, string.Empty),
                StockMovementService.OrigineTypeAvoir when m.OrigineId is int avoirId => avoirMap.GetValueOrDefault(avoirId, string.Empty),
                StockMovementService.OrigineTypeAvoirFournisseur when m.OrigineId is int avfId => avoirFournisseurMap.GetValueOrDefault(avfId, string.Empty),
                _ => string.Empty
            };
            m.PartyIsSupplier = m.OrigineType is StockMovementService.OrigineTypeBonReception
                or StockMovementService.OrigineTypeAvoirFournisseur;

            decimal? price = null;
            if (m.OrigineId is int docId)
            {
                price = m.OrigineType switch
                {
                    StockMovementService.OrigineTypeBonLivraison when blPriceMap.TryGetValue((docId, m.ProduitId), out var blP) => blP,
                    StockMovementService.OrigineTypeBonReception when brPriceMap.TryGetValue((docId, m.ProduitId), out var brP) => brP,
                    StockMovementService.OrigineTypeAvoir when avoirPriceMap.TryGetValue((docId, m.ProduitId), out var avP) => avP,
                    StockMovementService.OrigineTypeAvoirFournisseur when avoirFournisseurPriceMap.TryGetValue((docId, m.ProduitId), out var avfP) => avfP,
                    _ => null
                };
            }
            m.UnitPriceDetail = price is decimal p
                ? $"{prixHtLabel} : {p.ToString("N2", System.Globalization.CultureInfo.CurrentCulture)}"
                : string.Empty;
        }
    }

    public static void ApplyLocationLabels(IReadOnlyList<MouvementStock> movements, ILocaleService locale)
    {
        var outside = locale.T("Lbl_StockOutside");
        var typeEntree = locale.T("TypeMvt_Entree");
        var typeSortie = locale.T("TypeMvt_Sortie");
        var typeAjust = locale.T("TypeMvt_Ajustement");
        var typeTransfert = locale.T("TypeMvt_Transfert");

        foreach (var m in movements)
        {
            m.FromLocationLabel = m.FromLocation?.Nom ?? (m.FromLocationId is null ? outside : $"#{m.FromLocationId}");
            m.ToLocationLabel = m.ToLocation?.Nom ?? (m.ToLocationId is null ? outside : $"#{m.ToLocationId}");
            m.TypeLabel = m.Type switch
            {
                TypeMouvement.Entree => typeEntree,
                TypeMouvement.Sortie => typeSortie,
                TypeMouvement.Transfert => typeTransfert,
                _ => typeAjust
            };
        }
    }
}
