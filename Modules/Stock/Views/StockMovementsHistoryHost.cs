using Avalonia.Controls.ApplicationLifetimes;
using GestionCommerciale.Modules.Stock.ViewModels;

namespace GestionCommerciale.Modules.Stock.Views;

public static class StockMovementsHistoryHost
{
    public static async Task ShowAsync(StockMovementsHistoryViewModel vm, CancellationToken ct = default)
    {
        var owner = Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        var w = new StockMovementsHistoryWindow { DataContext = vm };
        vm.RequestClose = () => w.Close();

        if (vm.LoadMouvementsCommand.CanExecute(null))
            await vm.LoadMouvementsCommand.ExecuteAsync(null);

        ct.ThrowIfCancellationRequested();

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();
    }
}
