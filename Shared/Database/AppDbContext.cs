using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Devis.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Modules.AvoirFournisseur.Models;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Modules.CommandeFournisseur.Models;
using GestionCommerciale.Modules.CommandeClient.Models;
using GestionCommerciale.Modules.FactureFournisseur.Models;
using GestionCommerciale.Modules.Personnel.Models;
using GestionCommerciale.Modules.Reception.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Modules.Tiers.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Shared.Database;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tiers> Tiers => Set<Tiers>();
    public DbSet<Categorie> Categories => Set<Categorie>();
    public DbSet<Produit> Produits => Set<Produit>();
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<MouvementStock> MouvementsStock => Set<MouvementStock>();
    public DbSet<Devis> Devis => Set<Devis>();
    public DbSet<DevisLigne> DevisLignes => Set<DevisLigne>();
    public DbSet<BonLivraison> BonsLivraison => Set<BonLivraison>();
    public DbSet<BonLivraisonLigne> BonLivraisonLignes => Set<BonLivraisonLigne>();
    public DbSet<PaiementBonLivraison> PaiementsBonLivraison => Set<PaiementBonLivraison>();
    public DbSet<BonCommande> BonsCommande => Set<BonCommande>();
    public DbSet<BonCommandeLigne> BonCommandeLignes => Set<BonCommandeLigne>();
    public DbSet<BonCommandeClient> BonsCommandeClient => Set<BonCommandeClient>();
    public DbSet<BonCommandeClientLigne> BonCommandeClientLignes => Set<BonCommandeClientLigne>();
    public DbSet<BonReception> BonsReception => Set<BonReception>();
    public DbSet<BonReceptionLigne> BonReceptionLignes => Set<BonReceptionLigne>();
    public DbSet<FactureFournisseur> FacturesFournisseurs => Set<FactureFournisseur>();
    public DbSet<FactureFournisseurLigne> FactureFournisseurLignes => Set<FactureFournisseurLigne>();
    public DbSet<PaiementFournisseur> PaiementsFournisseurs => Set<PaiementFournisseur>();
    public DbSet<Facture> Factures => Set<Facture>();
    public DbSet<FactureLigne> FactureLignes => Set<FactureLigne>();
    public DbSet<Avoir> Avoirs => Set<Avoir>();
    public DbSet<AvoirLigne> AvoirLignes => Set<AvoirLigne>();
    public DbSet<AvoirFournisseur> AvoirsFournisseurs => Set<AvoirFournisseur>();
    public DbSet<AvoirFournisseurLigne> AvoirFournisseurLignes => Set<AvoirFournisseurLigne>();
    public DbSet<TypeCharge> TypesCharges => Set<TypeCharge>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<BonCharge> BonsCharge => Set<BonCharge>();
    public DbSet<BonChargeLigne> BonChargeLignes => Set<BonChargeLigne>();
    public DbSet<BonDecharge> BonsDecharge => Set<BonDecharge>();
    public DbSet<BonDechargeLigne> BonDechargeLignes => Set<BonDechargeLigne>();
    public DbSet<RemiseCaisse> RemisesCaisse => Set<RemiseCaisse>();
    public DbSet<AppSettingsRow> AppSettings => Set<AppSettingsRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Phone).HasMaxLength(50).IsRequired();
            e.HasIndex(u => u.Phone).IsUnique();
            e.Property(u => u.UserType)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(UserType.Vendeur)
                .IsRequired();
            e.Property(u => u.Actif).HasDefaultValue(true);
            e.HasOne(u => u.VirtualStock)
                .WithOne(l => l.User!)
                .HasForeignKey<StockLocation>(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tiers>(e =>
        {
            e.ToTable("Tiers");
            e.Property(t => t.Type).HasConversion<int>();
            e.Ignore(t => t.NomEtSolde);
        });

        modelBuilder.Entity<Produit>(e =>
        {
            e.HasOne(p => p.Categorie).WithMany().HasForeignKey(p => p.CategorieId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(p => p.Reference).IsUnique();
            e.Ignore(p => p.StockActuel);
        });

        modelBuilder.Entity<StockLocation>(e =>
        {
            e.ToTable("StockLocations", t =>
            {
                t.HasCheckConstraint(
                    "CK_StockLocation_VirtualUser",
                    "(IsVirtual = 0 AND UserId IS NULL) OR (IsVirtual = 1 AND UserId IS NOT NULL)");
            });
            e.Property(l => l.Nom).HasMaxLength(200).IsRequired();
            e.HasIndex(l => l.UserId)
                .IsUnique()
                .HasFilter("IsVirtual = 1");
            e.HasData(new StockLocation
            {
                Id = 1,
                Nom = StockLocation.DefaultDepotNom,
                IsVirtual = false,
                UserId = null,
                Actif = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        });

        modelBuilder.Entity<MouvementStock>(e =>
        {
            e.HasOne(m => m.Produit).WithMany().HasForeignKey(m => m.ProduitId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.FromLocation).WithMany().HasForeignKey(m => m.FromLocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.ToLocation).WithMany().HasForeignKey(m => m.ToLocationId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(m => m.ProduitId);
            e.HasIndex(m => new { m.OrigineType, m.OrigineId });
            e.Ignore(m => m.Type);
            e.Ignore(m => m.HistoryLocationId);
            e.Ignore(m => m.StockApres);
            e.Ignore(m => m.SignedQuantite);
            e.Ignore(m => m.QuantiteSignedLabel);
            e.Ignore(m => m.PartyName);
            e.Ignore(m => m.PartyIsSupplier);
            e.Ignore(m => m.HasPartyName);
            e.Ignore(m => m.PartyColorSignal);
            e.Ignore(m => m.DocumentRef);
            e.Ignore(m => m.CanOpenOrigin);
            e.Ignore(m => m.TraceDetail);
            e.Ignore(m => m.UnitPriceDetail);
            e.Ignore(m => m.HasUnitPriceDetail);
        });

        modelBuilder.Entity<Devis>(e =>
        {
            e.HasMany(d => d.Lignes).WithOne(l => l.Devis).HasForeignKey(l => l.DevisId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Tiers>().WithMany().HasForeignKey(d => d.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.ClientId);
        });

        modelBuilder.Entity<BonLivraison>(e =>
        {
            e.HasMany(b => b.Lignes).WithOne(l => l.BonLivraison).HasForeignKey(l => l.BLId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(b => b.Paiements).WithOne(p => p.BonLivraison).HasForeignKey(p => p.BonLivraisonId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Facture).WithMany()
                .HasForeignKey(b => b.FactureId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne<BonCommandeClient>().WithMany()
                .HasForeignKey(b => b.BonCommandeClientId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Tiers>().WithMany().HasForeignKey(b => b.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Vendeur).WithMany()
                .HasForeignKey(b => b.VendeurId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(b => b.FactureId);
            e.HasIndex(b => b.BonCommandeClientId);
            e.HasIndex(b => b.ClientId);
            e.HasIndex(b => b.VendeurId);
        });

        modelBuilder.Entity<PaiementBonLivraison>(e =>
        {
            e.Property(p => p.Mode).HasConversion<int>();
        });

        modelBuilder.Entity<BonCommandeClient>(e =>
        {
            e.HasMany(b => b.Lignes).WithOne(l => l.BonCommandeClient).HasForeignKey(l => l.BonCommandeClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Facture).WithMany()
                .HasForeignKey(b => b.FactureId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Tiers>().WithMany().HasForeignKey(b => b.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(b => b.FactureId);
            e.HasIndex(b => b.ClientId);
        });

        modelBuilder.Entity<BonCommande>(e =>
        {
            e.HasMany(b => b.Lignes).WithOne(l => l.BonCommande).HasForeignKey(l => l.BonCommandeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Tiers>().WithMany().HasForeignKey(b => b.FournisseurId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(b => b.FournisseurId);
        });

        modelBuilder.Entity<BonReception>(e =>
        {
            e.HasOne(b => b.BonCommande).WithMany().HasForeignKey(b => b.BonCommandeId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(b => b.Lignes).WithOne(l => l.BonReception).HasForeignKey(l => l.BRId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<FactureFournisseur>().WithMany()
                .HasForeignKey(b => b.FactureFournisseurId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne<Tiers>().WithMany().HasForeignKey(b => b.FournisseurId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(b => b.FactureFournisseurId);
            e.HasIndex(b => b.FournisseurId);
        });

        modelBuilder.Entity<FactureFournisseur>(e =>
        {
            e.HasMany(f => f.Lignes).WithOne(l => l.FactureFournisseur).HasForeignKey(l => l.FactureFournisseurId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Paiements).WithOne(p => p.FactureFournisseur).HasForeignKey(p => p.FactureFournisseurId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Tiers>().WithMany().HasForeignKey(f => f.FournisseurId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(f => f.FournisseurId);
        });

        modelBuilder.Entity<FactureFournisseurLigne>(e =>
        {
            e.HasOne(l => l.BonReception).WithMany()
                .HasForeignKey(l => l.BonReceptionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(l => l.BonReceptionId);
        });

        modelBuilder.Entity<Facture>(e =>
        {
            e.HasMany(f => f.Lignes).WithOne(l => l.Facture).HasForeignKey(l => l.FactureId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Tiers>().WithMany().HasForeignKey(f => f.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(f => f.ClientId);
        });

        modelBuilder.Entity<FactureLigne>(e =>
        {
            e.HasOne(l => l.BonLivraison).WithMany()
                .HasForeignKey(l => l.BonLivraisonId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(l => l.BonLivraisonId);
        });

        modelBuilder.Entity<PaiementFournisseur>(e =>
        {
            e.Property(p => p.Mode).HasConversion<int>();
        });

        modelBuilder.Entity<Avoir>(e =>
        {
            e.HasOne(a => a.Facture).WithMany().HasForeignKey(a => a.FactureId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(a => a.Lignes).WithOne(l => l.Avoir).HasForeignKey(l => l.AvoirId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Tiers>().WithMany().HasForeignKey(a => a.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => a.ClientId);
        });

        modelBuilder.Entity<AvoirFournisseur>(e =>
        {
            e.HasMany(a => a.Lignes).WithOne(l => l.AvoirFournisseur).HasForeignKey(l => l.AvoirFournisseurId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Tiers>().WithMany().HasForeignKey(a => a.FournisseurId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => a.FournisseurId);
        });

        modelBuilder.Entity<TypeCharge>(e =>
        {
            e.Property(t => t.Nom).HasMaxLength(128).IsRequired();
            e.HasIndex(t => t.Nom).IsUnique();
        });

        modelBuilder.Entity<Charge>(e =>
        {
            e.Property(c => c.Libelle).HasMaxLength(256).IsRequired();
            e.HasOne(c => c.TypeCharge).WithMany().HasForeignKey(c => c.TypeChargeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(c => c.TypeChargeId);
            e.HasIndex(c => c.Date);
        });

        modelBuilder.Entity<BonCharge>(e =>
        {
            e.ToTable("BonsCharge");
            e.Property(b => b.Numero).HasMaxLength(50).IsRequired();
            e.HasIndex(b => b.Numero).IsUnique();
            e.Property(b => b.Note).HasMaxLength(1000);
            e.Property(b => b.DepotLocationId).HasDefaultValue(1);
            e.HasOne(b => b.AssignedToUser)
                .WithMany()
                .HasForeignKey(b => b.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.DepotLocation)
                .WithMany()
                .HasForeignKey(b => b.DepotLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(b => b.Lignes)
                .WithOne(l => l.BonCharge)
                .HasForeignKey(l => l.BonChargeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(b => b.AssignedToUserId);
            e.HasIndex(b => b.DepotLocationId);
        });

        modelBuilder.Entity<BonChargeLigne>(e =>
        {
            e.ToTable("BonChargeLignes");
            e.Property(l => l.Designation).HasMaxLength(300).IsRequired();
            e.HasOne<Produit>()
                .WithMany()
                .HasForeignKey(l => l.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => l.BonChargeId);
            e.HasIndex(l => l.ProduitId);
        });

        modelBuilder.Entity<BonDecharge>(e =>
        {
            e.ToTable("BonsDecharge");
            e.Property(b => b.Numero).HasMaxLength(50).IsRequired();
            e.HasIndex(b => b.Numero).IsUnique();
            e.Property(b => b.Note).HasMaxLength(1000);
            e.Property(b => b.DepotLocationId).HasDefaultValue(1);
            e.HasOne(b => b.AssignedToUser)
                .WithMany()
                .HasForeignKey(b => b.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.DepotLocation)
                .WithMany()
                .HasForeignKey(b => b.DepotLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(b => b.Lignes)
                .WithOne(l => l.BonDecharge)
                .HasForeignKey(l => l.BonDechargeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(b => b.AssignedToUserId);
            e.HasIndex(b => b.DepotLocationId);
        });

        modelBuilder.Entity<BonDechargeLigne>(e =>
        {
            e.ToTable("BonDechargeLignes");
            e.Property(l => l.Designation).HasMaxLength(300).IsRequired();
            e.HasOne<Produit>()
                .WithMany()
                .HasForeignKey(l => l.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => l.BonDechargeId);
            e.HasIndex(l => l.ProduitId);
        });

        modelBuilder.Entity<RemiseCaisse>(e =>
        {
            e.ToTable("RemisesCaisse");
            e.Ignore(r => r.CreatedByUserId);
            e.Property(r => r.Numero).HasMaxLength(50).IsRequired();
            e.HasIndex(r => r.Numero).IsUnique();
            e.HasIndex(r => new { r.AssignedToUserId, r.Date });
            e.Property(r => r.Note).HasMaxLength(1000);
            e.Property(r => r.Mode).HasConversion<int>();
            e.HasOne(r => r.AssignedToUser)
                .WithMany()
                .HasForeignKey(r => r.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppSettingsRow>(e =>
        {
            e.HasKey(x => x.Id);
        });
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var utc = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<GestionCommerciale.Shared.Models.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utc;
                entry.Entity.UpdatedAt = utc;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utc;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AppSettingsRow>())
        {
            // no BaseEntity timestamps
        }
    }
}
