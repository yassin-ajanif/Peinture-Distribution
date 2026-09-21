using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Personnel.Models;

namespace GestionCommerciale.Modules.Personnel.Services;

public interface IVendeurCaisseService
{
    Task<VendeurCaisseSummary> GetSummaryAsync(int vendeurId, CancellationToken cancellationToken = default);

    Task<RemiseCaisse> CreateRemiseAsync(
        int vendeurId,
        DateTime date,
        decimal montant,
        ModePaiement mode,
        string note,
        CancellationToken cancellationToken = default);

    Task DeleteRemiseAsync(int remiseId, CancellationToken cancellationToken = default);
}
