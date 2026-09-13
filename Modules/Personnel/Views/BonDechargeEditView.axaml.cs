using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GestionCommerciale.Modules.Personnel.Views;

public partial class BonDechargeEditView : UserControl
{
    public BonDechargeEditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
