using GestionCommerciale.Modules.Stock.Models;

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
}
