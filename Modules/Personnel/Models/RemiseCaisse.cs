using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Facturation.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Personnel.Models;

/// <summary>Cash hand-in: vendeur returns collected money to the company.</summary>
public class RemiseCaisse : BaseEntity
{
    public string Numero { get; set; } = string.Empty;

    /// <summary>Vendeur who remits cash (not audit).</summary>
    public int AssignedToUserId { get; set; }
    public User? AssignedToUser { get; set; }

    public DateTime Date { get; set; }
    public decimal Montant { get; set; }
    public ModePaiement Mode { get; set; }
    public string Note { get; set; } = string.Empty;
}
