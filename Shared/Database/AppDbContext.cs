using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Devis.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Modules.AvoirFournisseur.Models;
using GestionCommerciale.Modules.Charges.Models;
using GestionCommerciale.Modules.CommandeFournisseur.Models;
using GestionCommerciale.Modules.CommandeClient.Models;
using GestionCommerciale.Modules.FactureFournisseur.Models;
using GestionCommerciale.Modules.Preparation.Models;
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
    public DbSet<Paiement> Paiements => Set<Paiement>();
    public DbSet<BonPreparation> BonsPreparation => Set<BonPreparation>();
    public DbSet<BonPreparationLigne> BonPreparationLignes => Set<BonPreparationLigne>();
    public DbSet<PaiementBonPreparation> PaiementsBonPreparation => Set<PaiementBonPreparation>();
    public DbSet<Avoir> Avoirs => Set<Avoir>();
    public DbSet<AvoirLigne> AvoirLignes => Set<AvoirLigne>();
    public DbSet<AvoirFournisseur> AvoirsFournisseurs => Set<AvoirFournisseur>();
    public DbSet<AvoirFournisseurLigne> AvoirFournisseurLignes => Set<AvoirFournisseurLigne>();
    public DbSet<TypeCharge> TypesCharges => Set<TypeCharge>();
    public DbSet<Charge> Charges => Set<Charge>();
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
            e.ToTable("Tiers", t =>
            {
                t.HasCheckConstraint("CK_Tiers_Categorie", "Categorie IN ('Officiel', 'Comptoir')");
            });
            e.Property(t => t.Type).HasConversion<int>();
            e.Property(t => t.Categorie)
                .HasConversion<string>()
                .HasMaxLength(32)
                .HasDefaultValue(CategorieTiers.Officiel)
                .IsRequired();
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
        });

        modelBuilder.Entity<BonLivraison>(e =>
        {
            e.HasMany(b => b.Lignes).WithOne(l => l.BonLivraison).HasForeignKey(l => l.BLId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Facture).WithMany()
                .HasForeignKey(b => b.FactureId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne<BonCommandeClient>().WithMany()
                .HasForeignKey(b => b.BonCommandeClientId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(b => b.FactureId);
            e.HasIndex(b => b.BonCommandeClientId);
        });

        modelBuilder.Entity<BonCommandeClient>(e =>
        {
            e.HasMany(b => b.Lignes).WithOne(l => l.BonCommandeClient).HasForeignKey(l => l.BonCommandeClientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Facture).WithMany()
                .HasForeignKey(b => b.FactureId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(b => b.FactureId);
        });

        modelBuilder.Entity<BonCommande>(e =>
        {
            e.HasMany(b => b.Lignes).WithOne(l => l.BonCommande).HasForeignKey(l => l.BonCommandeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BonReception>(e =>
        {
            e.HasOne(b => b.BonCommande).WithMany().HasForeignKey(b => b.BonCommandeId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(b => b.Lignes).WithOne(l => l.BonReception).HasForeignKey(l => l.BRId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<FactureFournisseur>().WithMany()
                .HasForeignKey(b => b.FactureFournisseurId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(b => b.FactureFournisseurId);
        });

        modelBuilder.Entity<FactureFournisseur>(e =>
        {
            e.HasMany(f => f.Lignes).WithOne(l => l.FactureFournisseur).HasForeignKey(l => l.FactureFournisseurId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Paiements).WithOne(p => p.FactureFournisseur).HasForeignKey(p => p.FactureFournisseurId).OnDelete(DeleteBehavior.Cascade);
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
            e.HasMany(f => f.Paiements).WithOne(p => p.Facture).HasForeignKey(p => p.FactureId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BonPreparation>(e =>
        {
            e.HasMany(f => f.Lignes).WithOne(l => l.BonPreparation).HasForeignKey(l => l.BonPreparationId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.Paiements).WithOne(p => p.BonPreparation).HasForeignKey(p => p.BonPreparationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.StockLocation).WithMany()
                .HasForeignKey(f => f.StockLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(f => f.StockLocationId).HasDefaultValue(1);
            e.HasIndex(f => f.StockLocationId);
        });

        modelBuilder.Entity<FactureLigne>(e =>
        {
            e.HasOne(l => l.BonLivraison).WithMany()
                .HasForeignKey(l => l.BonLivraisonId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(l => l.BonLivraisonId);
        });

        modelBuilder.Entity<Paiement>(e =>
        {
            e.Property(p => p.Mode).HasConversion<int>();
        });

        modelBuilder.Entity<PaiementBonPreparation>(e =>
        {
            e.Property(p => p.Mode).HasConversion<int>();
        });

        modelBuilder.Entity<PaiementFournisseur>(e =>
        {
            e.Property(p => p.Mode).HasConversion<int>();
        });

        modelBuilder.Entity<Avoir>(e =>
        {
            e.HasOne(a => a.Facture).WithMany().HasForeignKey(a => a.FactureId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(a => a.Lignes).WithOne(l => l.Avoir).HasForeignKey(l => l.AvoirId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AvoirFournisseur>(e =>
        {
            e.HasMany(a => a.Lignes).WithOne(l => l.AvoirFournisseur).HasForeignKey(l => l.AvoirFournisseurId).OnDelete(DeleteBehavior.Cascade);
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
