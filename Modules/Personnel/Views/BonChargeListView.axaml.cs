using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Personnel.Views;

public partial class BonChargeListView : UserControl
{
    public BonChargeListView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is ViewModels.BonChargeListViewModel vm)
            vm.LoadCommand.Execute(null);
    }

    private void OnRowContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is ContextMenu cm && cm.PlacementTarget is { DataContext: ViewModels.BonChargeListRow row })
        {
            if (DataContext is ViewModels.BonChargeListViewModel vm)
                vm.Selected = row;
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
