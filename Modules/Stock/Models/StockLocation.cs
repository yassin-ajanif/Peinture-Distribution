using GestionCommerciale.Modules.Auth.Models;
using GestionCommerciale.Shared.Models;

namespace GestionCommerciale.Modules.Stock.Models;

public class StockLocation : BaseEntity
{
    public const string DefaultDepotNom = "Dépôt principal";

    public string Nom { get; set; } = string.Empty;
    public bool IsVirtual { get; set; }
    /// <summary>Required when <see cref="IsVirtual"/>; null for physical depot.</summary>
    public int? UserId { get; set; }
    public bool Actif { get; set; } = true;

    public User? User { get; set; }
}
