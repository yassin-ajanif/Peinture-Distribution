using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Livraison.Models;

public class PaiementBonLivraison : BaseEntity
{
    public int BonLivraisonId { get; set; }
    public BonLivraison? BonLivraison { get; set; }
    public decimal Montant { get; set; }
    public DateTime Date { get; set; }
    public ModePaiement Mode { get; set; }
    public string Reference { get; set; } = string.Empty;
}
