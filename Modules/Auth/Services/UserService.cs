using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Auth.Services;

public interface IUserService
{
    Task<User> CreateVendeurAsync(string fullName, string phone, CancellationToken cancellationToken = default);
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

    public async Task<User> CreateVendeurAsync(
        string fullName,
        string phone,
        CancellationToken cancellationToken = default)
    {
        var phoneTrim = phone.Trim();
        if (string.IsNullOrWhiteSpace(phoneTrim))
            throw new ArgumentException("Le téléphone est requis.", nameof(phone));

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        if (await db.Users.AnyAsync(u => u.Phone == phoneTrim, cancellationToken))
            throw new InvalidOperationException("Ce numéro est déjà utilisé.");

        var user = new User
        {
            FullName = fullName.Trim(),
            Phone = phoneTrim,
            UserType = UserType.Vendeur,
            Actif = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await _locations.GetOrCreateVirtualForUserAsync(db, user, cancellationToken);
        return user;
    }
}
