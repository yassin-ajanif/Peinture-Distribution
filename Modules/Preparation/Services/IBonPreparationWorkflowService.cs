using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Preparation.Models;

namespace GestionCommerciale.Modules.Preparation.Services;

public interface IBonPreparationWorkflowService
{
    Task AddPaiementAsync(int factureId, PaiementBonPreparation paiement, CancellationToken cancellationToken = default);
    Task UpdatePaiementAsync(int factureId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default);
    Task DeletePaiementAsync(int factureId, int paiementId, CancellationToken cancellationToken = default);
}
