using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Livraison.Models;
using GestionCommerciale.Modules.Stock.Services;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Livraison.Services;

public sealed class BonLivraisonWorkflowService : IBonLivraisonWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IStockMovementService _stock;

    public BonLivraisonWorkflowService(IDbContextFactory<AppDbContext> dbFactory, IStockMovementService stock)
    {
        _dbFactory = dbFactory;
        _stock = stock;
    }

    public async Task ValiderAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var bl = await db.BonsLivraison.Include(b => b.Lignes).FirstAsync(b => b.Id == bonLivraisonId, cancellationToken);

        await _stock.ResyncBonLivraisonStockAsync(
            db,
            bonLivraisonId,
            bl.Numero,
            bl.Lignes.Select(l => (l.ProduitId, l.QuantiteLivree)),
            userId,
            cancellationToken);

        DocumentTotalsHelper.SyncBonLivraisonTotalTtc(bl);
        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }

    public async Task ResyncStockFromLinesAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken);

        var bl = await db.BonsLivraison.Include(b => b.Lignes).FirstAsync(b => b.Id == bonLivraisonId, cancellationToken);

        await _stock.ResyncBonLivraisonStockAsync(
            db,
            bonLivraisonId,
            bl.Numero,
            bl.Lignes.Select(l => (l.ProduitId, l.QuantiteLivree)),
            userId,
            cancellationToken);

        DocumentTotalsHelper.SyncBonLivraisonTotalTtc(bl);
        await db.SaveChangesAsync(cancellationToken);
        await trx.CommitAsync(cancellationToken);
    }

    public async Task AddPaiementAsync(int bonLivraisonId, PaiementBonLivraison paiement, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var bl = await db.BonsLivraison
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == bonLivraisonId, cancellationToken);

        DocumentTotalsHelper.SyncBonLivraisonTotalTtc(bl);
        var ttc = bl.TotalTtc;
        var totalApres = bl.Paiements.Sum(p => p.Montant) + paiement.Montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        paiement.BonLivraisonId = bonLivraisonId;
        db.PaiementsBonLivraison.Add(paiement);
        SyncEstPayee(bl);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePaiementAsync(int bonLivraisonId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default)
    {
        if (montant <= 0)
            throw new InvalidOperationException("Le montant doit être supérieur à 0.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var bl = await db.BonsLivraison
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == bonLivraisonId, cancellationToken);

        DocumentTotalsHelper.SyncBonLivraisonTotalTtc(bl);
        var ttc = bl.TotalTtc;
        var totalApres = bl.Paiements.Where(x => x.Id != paiementId).Sum(x => x.Montant) + montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        var p = await db.PaiementsBonLivraison.FirstAsync(x => x.Id == paiementId && x.BonLivraisonId == bonLivraisonId, cancellationToken);
        p.Montant = montant;
        p.Date = date;
        p.Mode = mode;
        p.Reference = reference;
        SyncEstPayee(bl);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePaiementAsync(int bonLivraisonId, int paiementId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var bl = await db.BonsLivraison
            .Include(x => x.Paiements)
            .FirstAsync(x => x.Id == bonLivraisonId, cancellationToken);
        var p = await db.PaiementsBonLivraison.FirstAsync(x => x.Id == paiementId && x.BonLivraisonId == bonLivraisonId, cancellationToken);
        db.PaiementsBonLivraison.Remove(p);
        bl.Paiements.Remove(p);
        SyncEstPayee(bl);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void SyncEstPayee(BonLivraison bl)
    {
        var paid = bl.Paiements.Where(p => p.Mode != ModePaiement.Credit).Sum(p => p.Montant);
        bl.EstPayee = bl.TotalTtc > 0 && paid >= bl.TotalTtc - DocumentTotalsHelper.PaiementTtcTolerance;
    }
}
