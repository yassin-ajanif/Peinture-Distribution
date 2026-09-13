using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Personnel.Views;

public partial class BonDechargeListView : UserControl
{
    public BonDechargeListView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is ViewModels.BonDechargeListViewModel vm)
            vm.LoadCommand.Execute(null);
    }

    private void OnRowContextMenuOpening(object? sender, CancelEventArgs e)
    {
        if (sender is ContextMenu cm && cm.PlacementTarget is { DataContext: ViewModels.BonDechargeListRow row })
        {
            if (DataContext is ViewModels.BonDechargeListViewModel vm)
                vm.Selected = row;
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
