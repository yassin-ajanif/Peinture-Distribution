using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Stock;

/// <summary>
/// Compatibility helpers for call sites without DI. Prefer <see cref="IStockRetrievalService"/>.
/// </summary>
public static class StockBalanceQueries
{
    private static readonly StockRetrievalService Retrieval = new();

    public static Task<decimal> GetBalanceAsync(
        AppDbContext db,
        int produitId,
        int locationId,
        CancellationToken cancellationToken = default)
        => Retrieval.GetStockAsync(db, produitId, locationId, cancellationToken);

    public static async Task<Dictionary<int, decimal>> GetTotalBalancesAsync(
        AppDbContext db,
        IEnumerable<int> produitIds,
        CancellationToken cancellationToken = default)
    {
        var balances = await Retrieval.GetTotalStocksAsync(db, produitIds, cancellationToken);
        return balances as Dictionary<int, decimal> ?? new Dictionary<int, decimal>(balances);
    }

    public static async Task HydrateStockActuelAsync(
        AppDbContext db,
        IReadOnlyList<Produit> produits,
        CancellationToken cancellationToken = default)
    {
        if (produits.Count == 0)
            return;

        var balances = await GetTotalBalancesAsync(db, produits.Select(p => p.Id), cancellationToken);
        foreach (var p in produits)
            p.StockActuel = balances.GetValueOrDefault(p.Id);
    }
}
