using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Personnel.Models;

/// <summary>Delivery of goods from physical depot to an assigned user's virtual stock.</summary>
public class BonCharge : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    /// <summary>User who receives the stock (vendeur now; later livreur, etc.).</summary>
    public int AssignedToUserId { get; set; }

    /// <summary>Physical depot the goods leave from. Defaults to dépôt principal.</summary>
    public int DepotLocationId { get; set; } = 1;

    public string Note { get; set; } = string.Empty;

    public User? AssignedToUser { get; set; }
    public StockLocation? DepotLocation { get; set; }
    public List<BonChargeLigne> Lignes { get; set; } = [];
}
