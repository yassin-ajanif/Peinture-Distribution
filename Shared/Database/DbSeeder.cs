using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Stock.Models;

namespace GestionCommerciale.Shared.Database;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@local";
    public const string DefaultAdminPassword = "admin";
    public const string DefaultClientName = "Client Comptoire";

    /// <summary>Seeded admin row representing the physical dépôt principal (shown on Vendeurs).</summary>
    public const string DepotPrincipalAdminPhone = "DEPOT-PRINCIPAL";
    public const string DepotPrincipalAdminName = "admin";

    public static bool IsDepotPrincipalAdmin(User? user) =>
        user is not null && IsDepotPrincipalAdminPhone(user.Phone);

    public static bool IsDepotPrincipalAdminPhone(string? phone) =>
        !string.IsNullOrWhiteSpace(phone)
        && phone.Trim().Equals(DepotPrincipalAdminPhone, StringComparison.OrdinalIgnoreCase);

    public static void Seed(AppDbContext db)
    {
        if (!db.AppSettings.Any())
        {
            db.AppSettings.Add(new AppSettingsRow { Id = 1 });
            db.SaveChanges();
        }

        if (!db.Tiers.Any(t => t.Nom == DefaultClientName))
        {
            db.Tiers.Add(new GestionCommerciale.Modules.Tiers.Models.Tiers
            {
                Nom = DefaultClientName,
                Type = GestionCommerciale.Modules.Tiers.Models.TypeTiers.Client,
                Categorie = GestionCommerciale.Modules.Tiers.Models.CategorieTiers.Comptoir,
                Actif = true
            });
            db.SaveChanges();
        }

        EnsureDepotPrincipalAdmin(db);
    }

    /// <summary>
    /// Ensures a fixed Admin user exists for the physical dépôt principal.
    /// Stock for this user is the dépôt (not a virtual location).
    /// </summary>
    public static void EnsureDepotPrincipalAdmin(AppDbContext db)
    {
        var depot = db.StockLocations.FirstOrDefault(l =>
            !l.IsVirtual && l.Nom == StockLocation.DefaultDepotNom);

        if (depot is null)
        {
            depot = db.StockLocations.FirstOrDefault(l => !l.IsVirtual && l.Actif);
        }

        if (depot is null)
        {
            depot = new StockLocation
            {
                Nom = StockLocation.DefaultDepotNom,
                IsVirtual = false,
                UserId = null,
                Actif = true
            };
            db.StockLocations.Add(depot);
            db.SaveChanges();
        }
        else if (!depot.Actif || depot.Nom != StockLocation.DefaultDepotNom)
        {
            depot.Nom = StockLocation.DefaultDepotNom;
            depot.Actif = true;
            depot.IsVirtual = false;
            depot.UserId = null;
            db.SaveChanges();
        }

        var admin = db.Users.FirstOrDefault(u => u.Phone == DepotPrincipalAdminPhone);
        if (admin is null)
        {
            db.Users.Add(new User
            {
                FullName = DepotPrincipalAdminName,
                Phone = DepotPrincipalAdminPhone,
                UserType = UserType.Admin,
                Actif = true,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
            return;
        }

        var dirty = false;
        if (admin.FullName != DepotPrincipalAdminName)
        {
            admin.FullName = DepotPrincipalAdminName;
            dirty = true;
        }

        if (admin.UserType != UserType.Admin)
        {
            admin.UserType = UserType.Admin;
            dirty = true;
        }

        if (!admin.Actif)
        {
            admin.Actif = true;
            dirty = true;
        }

        // Never keep a virtual stock for the depot admin — stock lives on the physical dépôt.
        var strayVirtual = db.StockLocations
            .FirstOrDefault(l => l.IsVirtual && l.UserId == admin.Id);
        if (strayVirtual is not null)
        {
            var hasMovements = db.MouvementsStock.Any(m =>
                m.FromLocationId == strayVirtual.Id || m.ToLocationId == strayVirtual.Id);
            if (!hasMovements)
                db.StockLocations.Remove(strayVirtual);
            dirty = true;
        }

        if (dirty)
            db.SaveChanges();
    }
}
