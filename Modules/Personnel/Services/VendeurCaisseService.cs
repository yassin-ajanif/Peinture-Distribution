using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Personnel.Models;
using GestionCommerciale.Shared.Database;
using GestionCommerciale.Shared.Helpers;
using GestionCommerciale.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Modules.Personnel.Services;

public sealed class VendeurCaisseService : IVendeurCaisseService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IDocumentNumberService _numbers;
    private readonly ILocaleService _locale;
    private readonly IAppSettingsService _settings;

    public VendeurCaisseService(
        IDbContextFactory<AppDbContext> dbFactory,
        IDocumentNumberService numbers,
        ILocaleService locale,
        IAppSettingsService settings)
    {
        _dbFactory = dbFactory;
        _numbers = numbers;
        _locale = locale;
        _settings = settings;
    }

    public async Task<VendeurCaisseSummary> GetSummaryAsync(int vendeurId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var ventes = await db.BonsLivraison.AsNoTracking()
            .Where(b => b.VendeurId == vendeurId)
            .SumAsync(b => (decimal?)b.TotalTtc, cancellationToken) ?? 0m;

        var encaisse = await (
            from p in db.PaiementsBonLivraison.AsNoTracking()
            join b in db.BonsLivraison.AsNoTracking() on p.BonLivraisonId equals b.Id
            where b.VendeurId == vendeurId && p.Mode != ModePaiement.Credit
            select p.Montant
        ).SumAsync(cancellationToken);

        var venteBls = await (
            from b in db.BonsLivraison.AsNoTracking()
            join t in db.Tiers.AsNoTracking() on b.ClientId equals t.Id into clients
            from t in clients.DefaultIfEmpty()
            where b.VendeurId == vendeurId
            orderby b.Date descending, b.Id descending
            select new
            {
                b.Id,
                b.Numero,
                b.Date,
                b.TotalTtc,
                ClientNom = t != null ? t.Nom : string.Empty
            }
        ).ToListAsync(cancellationToken);

        var encaissements = await (
            from p in db.PaiementsBonLivraison.AsNoTracking()
            join b in db.BonsLivraison.AsNoTracking() on p.BonLivraisonId equals b.Id
            where b.VendeurId == vendeurId && p.Mode != ModePaiement.Credit
            orderby p.Date descending, p.Id descending
            select new
            {
                p.Id,
                p.BonLivraisonId,
                b.Numero,
                BlDate = b.Date,
                p.Date,
                p.Mode,
                p.Montant
            }
        ).ToListAsync(cancellationToken);

        var remises = await db.RemisesCaisse.AsNoTracking()
            .Where(r => r.AssignedToUserId == vendeurId)
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.Id)
            .ToListAsync(cancellationToken);

        var remis = remises.Sum(r => r.Montant);
        var cfg = await _settings.GetAsync(cancellationToken);
        var currency = CurrencyHelper.FromSettings(cfg);
        if (string.IsNullOrEmpty(currency))
            currency = "DH";

        var venteRows = venteBls.Select(b => new VendeurVenteBlRow
        {
            Id = b.Id,
            Numero = b.Numero,
            Date = b.Date,
            ClientNom = b.ClientNom,
            Montant = b.TotalTtc,
            DateLabel = b.Date.ToString("d"),
            MontantLabel = CurrencyHelper.Format(b.TotalTtc, currency)
        }).ToList();

        var encaisseRows = encaissements.Select(p => new VendeurEncaisseRow
        {
            PaiementId = p.Id,
            BonLivraisonId = p.BonLivraisonId,
            BlNumero = p.Numero,
            BlDate = p.BlDate,
            Date = p.Date,
            Mode = p.Mode,
            Montant = p.Montant,
            DateLabel = p.Date.ToString("d"),
            MontantLabel = CurrencyHelper.Format(p.Montant, currency),
            ModeLabel = UiEnumStrings.FormatModePaiement(_locale, p.Mode)
        }).ToList();

        var rows = remises.Select(r => new VendeurRemiseRow
        {
            Id = r.Id,
            Numero = r.Numero,
            Date = r.Date,
            Montant = r.Montant,
            Mode = r.Mode,
            Note = r.Note,
            DateLabel = r.Date.ToString("d"),
            MontantLabel = CurrencyHelper.Format(r.Montant, currency),
            ModeLabel = UiEnumStrings.FormatModePaiement(_locale, r.Mode)
        }).ToList();

        return new VendeurCaisseSummary
        {
            Ventes = ventes,
            Encaisse = encaisse,
            Remis = remis,
            VenteBls = venteRows,
            Encaissements = encaisseRows,
            Remises = rows
        };
    }

    public async Task<RemiseCaisse> CreateRemiseAsync(
        int vendeurId,
        DateTime date,
        decimal montant,
        ModePaiement mode,
        string note,
        CancellationToken cancellationToken = default)
    {
        if (montant <= 0m)
            throw new InvalidOperationException(_locale.T("VendeurCaisse_ErrMontant"));

        if (mode == ModePaiement.Credit)
            throw new InvalidOperationException(_locale.T("VendeurCaisse_ErrModeCredit"));

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var exists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Id == vendeurId && u.Actif, cancellationToken);
        if (!exists)
            throw new InvalidOperationException(_locale.T("VendeurCaisse_ErrVendeur"));

        var ventes = await db.BonsLivraison.AsNoTracking()
            .Where(b => b.VendeurId == vendeurId)
            .SumAsync(b => (decimal?)b.TotalTtc, cancellationToken) ?? 0m;

        var remis = await db.RemisesCaisse.AsNoTracking()
            .Where(r => r.AssignedToUserId == vendeurId)
            .SumAsync(r => (decimal?)r.Montant, cancellationToken) ?? 0m;

        var aRemettreRestant = ventes - remis;
        if (montant > aRemettreRestant)
        {
            var cfg = await _settings.GetAsync(cancellationToken);
            var currency = CurrencyHelper.FromSettings(cfg);
            if (string.IsNullOrEmpty(currency))
                currency = "DH";

            var maxLabel = CurrencyHelper.Format(Math.Max(0m, aRemettreRestant), currency);
            throw new InvalidOperationException(_locale.Tf("VendeurCaisse_ErrExceedsARemettre", maxLabel));
        }

        var entity = new RemiseCaisse
        {
            Numero = await _numbers.NextRemiseCaisseAsync(cancellationToken),
            AssignedToUserId = vendeurId,
            Date = date.Date,
            Montant = montant,
            Mode = mode,
            Note = note?.Trim() ?? string.Empty
        };

        db.RemisesCaisse.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task DeleteRemiseAsync(int remiseId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.RemisesCaisse.FirstOrDefaultAsync(r => r.Id == remiseId, cancellationToken);
        if (entity is null)
            return;

        db.RemisesCaisse.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
