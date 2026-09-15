using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GestionCommerciale.Modules.Tiers.Models;

namespace GestionCommerciale.Shared.Helpers;

/// <summary>Holds the client list for document pickers (flat — no Officiel/Comptoir split).</summary>
public partial class ClientCategoryFilter : ObservableObject
{
    public ObservableCollection<Tiers> Clients { get; } = [];

    public void ReplaceAll(IEnumerable<Tiers> clients)
    {
        Clients.Clear();
        foreach (var c in clients.OrderBy(x => x.Nom))
        {
            c.ResetNomEtSolde();
            Clients.Add(c);
        }
    }
}
