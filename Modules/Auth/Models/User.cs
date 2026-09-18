using System.ComponentModel.DataAnnotations.Schema;
using GestionCommerciale.Modules.Stock.Models;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Modules.Auth.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    /// <summary>Login / phone number (unique).</summary>
    public string Phone { get; set; } = string.Empty;
    public UserType UserType { get; set; } = UserType.Vendeur;
    public bool Actif { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public StockLocation? VirtualStock { get; set; }

    /// <summary>UI label; dépôt principal admin is shown as "admin — Dépôt principal".</summary>
    [NotMapped]
    public string DisplayLabel => DbSeeder.FormatUserDisplayName(this);
}
