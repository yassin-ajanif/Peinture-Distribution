using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Modules.Livraison.Models;

namespace GestionCommerciale.Modules.Livraison.Services;

public interface IBonLivraisonWorkflowService
{
    Task ValiderAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default);

    /// <summary>Re-applies stock movements from current BL lines (idempotent).</summary>
    Task ResyncStockFromLinesAsync(int bonLivraisonId, int? userId, CancellationToken cancellationToken = default);

    Task AddPaiementAsync(int bonLivraisonId, PaiementBonLivraison paiement, CancellationToken cancellationToken = default);
    Task UpdatePaiementAsync(int bonLivraisonId, int paiementId, decimal montant, DateTime date, ModePaiement mode, string reference, CancellationToken cancellationToken = default);
    Task DeletePaiementAsync(int bonLivraisonId, int paiementId, CancellationToken cancellationToken = default);
}
