using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Stock.Services;

public sealed class StockRetrievalService : IStockRetrievalService
{
    public async Task<decimal> GetStockAsync(
        AppDbContext db,
        int produitId,
        int locationId,
        CancellationToken cancellationToken = default)
    {
        var latest = await db.MouvementsStock.AsNoTracking()
            .Where(m => m.ProduitId == produitId
                        && (m.FromLocationId == locationId || m.ToLocationId == locationId))
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Select(m => new { m.FromLocationId, m.FromApres, m.ToLocationId, m.ToApres })
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
            return 0m;

        // Location was Vers → ToApres; location was De → FromApres
        if (latest.ToLocationId == locationId)
            return latest.ToApres ?? 0m;
        if (latest.FromLocationId == locationId)
            return latest.FromApres ?? 0m;
        return 0m;
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetStocksAsync(
        AppDbContext db,
        IEnumerable<int> produitIds,
        int locationId,
        CancellationToken cancellationToken = default)
    {
        var ids = produitIds.Distinct().ToList();
        var result = new Dictionary<int, decimal>(ids.Count);
        foreach (var produitId in ids)
            result[produitId] = await GetStockAsync(db, produitId, locationId, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyDictionary<int, decimal>> GetTotalStocksAsync(
        AppDbContext db,
        IEnumerable<int> produitIds,
        CancellationToken cancellationToken = default)
    {
        var ids = produitIds.Distinct().ToList();
        var result = ids.ToDictionary(id => id, _ => 0m);
        if (ids.Count == 0)
            return result;

        var locationIds = await db.StockLocations.AsNoTracking()
            .Where(l => l.Actif)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        foreach (var produitId in ids)
        {
            decimal total = 0m;
            foreach (var locationId in locationIds)
                total += await GetStockAsync(db, produitId, locationId, cancellationToken);
            result[produitId] = total;
        }

        return result;
    }

    public async Task HydrateProductStocksAsync(
        AppDbContext db,
        IReadOnlyList<Produit> produits,
        int locationId,
        CancellationToken cancellationToken = default)
    {
        if (produits.Count == 0)
            return;

        var balances = await GetStocksAsync(db, produits.Select(p => p.Id), locationId, cancellationToken);
        foreach (var p in produits)
            p.StockActuel = balances.GetValueOrDefault(p.Id);
    }
}
