using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Preparation.Models;

public class BonPreparation : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public DateTime Date { get; set; }
    public DateTime DateEcheance { get; set; }
    public bool EstPayee { get; set; }
    public decimal RemiseGlobale { get; set; }
    public decimal TotalTtc { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<BonPreparationLigne> Lignes { get; set; } = [];
    public List<PaiementBonPreparation> Paiements { get; set; } = [];
}
