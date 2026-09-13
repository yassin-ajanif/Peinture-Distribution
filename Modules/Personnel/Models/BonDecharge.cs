using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Personnel.Models;

/// <summary>Return of goods from an assigned user's virtual stock back to physical depot.</summary>
public class BonDecharge : BaseEntity
{
    public string Numero { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    /// <summary>User who returns the stock (vendeur now; later livreur, etc.).</summary>
    public int AssignedToUserId { get; set; }

    /// <summary>Physical depot the goods return to. Defaults to dépôt principal.</summary>
    public int DepotLocationId { get; set; } = 1;

    public string Note { get; set; } = string.Empty;

    public User? AssignedToUser { get; set; }
    public StockLocation? DepotLocation { get; set; }
    public List<BonDechargeLigne> Lignes { get; set; } = [];
}
