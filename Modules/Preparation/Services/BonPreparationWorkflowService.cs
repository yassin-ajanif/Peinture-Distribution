using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Preparation.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Preparation.Services;

public sealed class BonPreparationWorkflowService : IBonPreparationWorkflowService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BonPreparationWorkflowService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task AddPaiementAsync(int factureId, PaiementBonPreparation paiement, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.BonsPreparation
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == factureId, cancellationToken);

        DocumentTotalsHelper.SyncBonPreparationTotalTtc(f);
        var ttc = f.TotalTtc;
        var totalApres = f.Paiements.Sum(p => p.Montant) + paiement.Montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        paiement.BonPreparationId = factureId;
        db.PaiementsBonPreparation.Add(paiement);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePaiementAsync(int factureId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default)
    {
        if (montant <= 0)
            throw new InvalidOperationException("Le montant doit être supérieur à 0.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var f = await db.BonsPreparation
            .Include(x => x.Paiements)
            .Include(x => x.Lignes)
            .FirstAsync(x => x.Id == factureId, cancellationToken);

        DocumentTotalsHelper.SyncBonPreparationTotalTtc(f);
        var ttc = f.TotalTtc;
        var totalApres = f.Paiements.Where(x => x.Id != paiementId).Sum(x => x.Montant) + montant;
        DocumentTotalsHelper.EnsurePaymentsNotOverTtc(ttc, totalApres);

        var p = await db.PaiementsBonPreparation.FirstAsync(x => x.Id == paiementId && x.BonPreparationId == factureId, cancellationToken);
        p.Montant = montant;
        p.Date = date;
        p.Mode = mode;
        p.Reference = reference;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeletePaiementAsync(int factureId, int paiementId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var p = await db.PaiementsBonPreparation.FirstAsync(x => x.Id == paiementId && x.BonPreparationId == factureId, cancellationToken);
        db.PaiementsBonPreparation.Remove(p);
        await db.SaveChangesAsync(cancellationToken);
    }
}
