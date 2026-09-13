using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Preparation.Models;

public class BonPreparationLigne : BaseEntity
{
    public int BonPreparationId { get; set; }
    public BonPreparation? BonPreparation { get; set; }
    public int ProduitId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public decimal Quantite { get; set; }
    public decimal PrixUnitaireHT { get; set; }
    public decimal Remise { get; set; }
    public decimal TauxTVA { get; set; }
    /// <summary>Unit / packaging label (e.g. carton, pièce).</summary>
    public string Conditionnement { get; set; } = string.Empty;
}
