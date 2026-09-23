using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GestionCommerciale.Modules.Auth.ViewModels;

namespace GestionCommerciale.Modules.Auth.Views;

public partial class VendeursView : UserControl
{
    private VendeursViewModel? _viewModel;

    public VendeursView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is VendeursViewModel vm)
            vm.LoadCommand.Execute(null);
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeViewModel();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        UnsubscribeViewModel();
        _viewModel = DataContext as VendeursViewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void UnsubscribeViewModel()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = null;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VendeursViewModel.SelectedCaisseDetailTab)
            && _viewModel?.SelectedCaisseDetailTab != VendeurCaisseDetailTab.Remis)
            DeactivateRemiseList();
    }

    private void OnRemiseListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        RemiseListBox?.Focus(NavigationMethod.Pointer);
    }

    private void OnRemiseListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RemiseListBox?.SelectedItem is null)
            return;

        Dispatcher.UIThread.Post(EnsureRemiseListFocus, DispatcherPriority.Input);
    }

    private void EnsureRemiseListFocus()
    {
        if (RemiseListBox is null || RemiseListBox.SelectedItem is null)
            return;

        if (!RemiseListBox.IsKeyboardFocusWithin)
            RemiseListBox.Focus(NavigationMethod.Pointer);
    }

    private void OnRemiseListLostFocus(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(ClearRemiseSelectionIfFocusLeft, DispatcherPriority.Input);
    }

    private void DeactivateRemiseList()
    {
        if (RemiseListBox is null)
            return;

        if (RemiseListBox.IsKeyboardFocusWithin)
            TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
    }

    private void ClearRemiseSelectionIfFocusLeft()
    {
        if (_viewModel?.SelectedRemise is null || RemiseListBox is null)
            return;

        if (_viewModel.SelectedCaisseDetailTab != VendeurCaisseDetailTab.Remis)
        {
            _viewModel.SelectedRemise = null;
            return;
        }

        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
        if (focused is not null && RemiseListBox.IsVisualAncestorOf(focused))
            return;

        if (focused is not null && DeleteRemiseButton.IsVisualAncestorOf(focused))
            return;

        _viewModel.SelectedRemise = null;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
