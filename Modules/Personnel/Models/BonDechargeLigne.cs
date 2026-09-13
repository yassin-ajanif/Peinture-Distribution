using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Personnel.Models;

public class BonDechargeLigne : BaseEntity
{
    public int BonDechargeId { get; set; }
    public BonDecharge? BonDecharge { get; set; }
    public int ProduitId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
}
