using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Stock.Services;

public interface IStockRetrievalService
{
    /// <summary>
    /// Current qty of a product at one location:
    /// last movement touching the location → ToApres if Vers, else FromApres.
    /// </summary>
    Task<decimal> GetStockAsync(
        AppDbContext db,
        int produitId,
        int locationId,
        CancellationToken cancellationToken = default);

    /// <summary>Current qty per product at one location.</summary>
    Task<IReadOnlyDictionary<int, decimal>> GetStocksAsync(
        AppDbContext db,
        IEnumerable<int> produitIds,
        int locationId,
        CancellationToken cancellationToken = default);

    /// <summary>Sum of stock across all active locations (company total).</summary>
    Task<IReadOnlyDictionary<int, decimal>> GetTotalStocksAsync(
        AppDbContext db,
        IEnumerable<int> produitIds,
        CancellationToken cancellationToken = default);

    Task HydrateProductStocksAsync(
        AppDbContext db,
        IReadOnlyList<Produit> produits,
        int locationId,
        CancellationToken cancellationToken = default);
}
