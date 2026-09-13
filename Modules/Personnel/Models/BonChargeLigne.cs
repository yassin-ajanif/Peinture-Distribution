using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Personnel.Models;

public class BonChargeLigne : BaseEntity
{
    public int BonChargeId { get; set; }
    public BonCharge? BonCharge { get; set; }
    public int ProduitId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
}
