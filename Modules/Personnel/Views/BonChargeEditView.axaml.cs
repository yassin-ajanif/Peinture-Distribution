using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Personnel.Views;

public partial class BonChargeEditView : UserControl
{
    public BonChargeEditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
