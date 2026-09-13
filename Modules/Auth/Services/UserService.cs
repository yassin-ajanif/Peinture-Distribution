using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Auth.Services;

public interface IUserService
{
    Task<IReadOnlyList<User>> ListVendeursAsync(string? search, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User> CreateVendeurAsync(string fullName, string phone, bool actif = true, CancellationToken cancellationToken = default);
    Task UpdateVendeurAsync(int id, string fullName, string phone, bool actif, CancellationToken cancellationToken = default);
    Task DeleteVendeurAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class UserService : IUserService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockLocationService _locations;

    public UserService(IDbContextFactory<AppDbContext> dbFactory, IStockLocationService locations)
    {
        _dbFactory = dbFactory;
        _locations = locations;
    }

    public async Task<IReadOnlyList<User>> ListVendeursAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var q = db.Users.AsNoTracking()
            .Where(u => u.UserType == UserType.Vendeur);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim().ToLowerInvariant();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(t) ||
                u.Phone.ToLower().Contains(t));
        }

        return await q
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Phone)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.UserType == UserType.Vendeur, cancellationToken);
    }

    public async Task<User> CreateVendeurAsync(
        string fullName,
        string phone,
        bool actif = true,
        CancellationToken cancellationToken = default)
    {
        var phoneTrim = RequirePhone(phone);
        var nameTrim = RequireName(fullName);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (await db.Users.AnyAsync(u => u.Phone == phoneTrim, cancellationToken))
            throw new InvalidOperationException("Ce numéro est déjà utilisé.");

        var user = new User
        {
            FullName = nameTrim,
            Phone = phoneTrim,
            UserType = UserType.Vendeur,
            Actif = actif,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await _locations.GetOrCreateVirtualForUserAsync(db, user, cancellationToken);
        return user;
    }

    public async Task UpdateVendeurAsync(
        int id,
        string fullName,
        string phone,
        bool actif,
        CancellationToken cancellationToken = default)
    {
        var phoneTrim = RequirePhone(phone);
        var nameTrim = RequireName(fullName);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType == UserType.Vendeur, cancellationToken)
            ?? throw new KeyNotFoundException("Vendeur introuvable.");

        if (await db.Users.AnyAsync(u => u.Phone == phoneTrim && u.Id != id, cancellationToken))
            throw new InvalidOperationException("Ce numéro est déjà utilisé.");

        user.FullName = nameTrim;
        user.Phone = phoneTrim;
        user.Actif = actif;

        var virtualStock = await db.StockLocations
            .FirstOrDefaultAsync(l => l.IsVirtual && l.UserId == id, cancellationToken);
        if (virtualStock is not null)
        {
            virtualStock.Nom = nameTrim;
            virtualStock.Actif = actif;
        }
        else
        {
            await _locations.GetOrCreateVirtualForUserAsync(db, user, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteVendeurAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.UserType == UserType.Vendeur, cancellationToken)
            ?? throw new KeyNotFoundException("Vendeur introuvable.");

        var virtualStock = await db.StockLocations
            .FirstOrDefaultAsync(l => l.IsVirtual && l.UserId == id, cancellationToken);

        if (virtualStock is not null)
        {
            var hasMovements = await db.MouvementsStock.AnyAsync(
                m => m.FromLocationId == virtualStock.Id || m.ToLocationId == virtualStock.Id,
                cancellationToken);
            if (hasMovements)
            {
                throw new InvalidOperationException(
                    "Impossible de supprimer ce vendeur : son stock virtuel a des mouvements. Désactivez-le à la place.");
            }

            db.StockLocations.Remove(virtualStock);
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string RequirePhone(string phone)
    {
        var phoneTrim = phone.Trim();
        if (string.IsNullOrWhiteSpace(phoneTrim))
            throw new ArgumentException("Le téléphone est requis.", nameof(phone));
        return phoneTrim;
    }

    private static string RequireName(string fullName)
    {
        var nameTrim = fullName.Trim();
        if (string.IsNullOrWhiteSpace(nameTrim))
            throw new ArgumentException("Le nom est requis.", nameof(fullName));
        return nameTrim;
    }
}
