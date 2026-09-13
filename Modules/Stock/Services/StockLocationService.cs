using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Stock.Services;

public interface IStockLocationService
{
    Task<StockLocation> GetOrCreateDefaultDepotAsync(AppDbContext db, CancellationToken cancellationToken = default);
    Task<StockLocation> GetOrCreateVirtualForUserAsync(AppDbContext db, User user, CancellationToken cancellationToken = default);
}

public sealed class StockLocationService : IStockLocationService
{
    public async Task<StockLocation> GetOrCreateDefaultDepotAsync(
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        var depot = await db.StockLocations
            .FirstOrDefaultAsync(l => !l.IsVirtual && l.Nom == StockLocation.DefaultDepotNom, cancellationToken);

        if (depot is not null)
            return depot;

        depot = await db.StockLocations
            .FirstOrDefaultAsync(l => !l.IsVirtual && l.Actif, cancellationToken);

        if (depot is not null)
            return depot;

        depot = new StockLocation
        {
            Nom = StockLocation.DefaultDepotNom,
            IsVirtual = false,
            UserId = null,
            Actif = true
        };
        db.StockLocations.Add(depot);
        await db.SaveChangesAsync(cancellationToken);
        return depot;
    }

    public async Task<StockLocation> GetOrCreateVirtualForUserAsync(
        AppDbContext db,
        User user,
        CancellationToken cancellationToken = default)
    {
        var existing = await db.StockLocations
            .FirstOrDefaultAsync(l => l.IsVirtual && l.UserId == user.Id, cancellationToken);

        if (existing is not null)
            return existing;

        var nom = string.IsNullOrWhiteSpace(user.FullName) ? user.Phone : user.FullName.Trim();
        var location = new StockLocation
        {
            Nom = nom,
            IsVirtual = true,
            UserId = user.Id,
            Actif = true
        };
        db.StockLocations.Add(location);
        await db.SaveChangesAsync(cancellationToken);
        return location;
    }
}
