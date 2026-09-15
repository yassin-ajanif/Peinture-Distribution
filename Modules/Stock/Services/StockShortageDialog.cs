using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Stock.Services;

public static class StockShortageDialog
{
    private static readonly IBrush DemandeBrush = new SolidColorBrush(Color.Parse("#C2410C"));
    private static readonly IBrush DisponibleBrush = new SolidColorBrush(Color.Parse("#0369A1"));
    private static readonly IBrush ManqueBrush = new SolidColorBrush(Color.Parse("#DC2626"));

    public static async Task<bool> ConfirmContinueAsync(
        IDialogService dialog,
        ILocaleService locale,
        IReadOnlyList<StockShortageItem> shortages,
        string title,
        bool block,
        CancellationToken cancellationToken = default)
    {
        if (shortages.Count == 0)
            return true;

        cancellationToken.ThrowIfCancellationRequested();
        return await ShowAsync(locale, shortages, title, block);
    }

    private static async Task<bool> ShowAsync(
        ILocaleService locale,
        IReadOnlyList<StockShortageItem> shortages,
        string title,
        bool block)
    {
        var owner = Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        var w = new Window
        {
            Title = title,
            MinWidth = 360,
            MaxWidth = 520,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var confirmed = false;
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 14, MaxWidth = 480 };

        panel.Children.Add(new TextBlock
        {
            Text = locale.Tf("Stock_ShortageHeader", shortages[0].LocationName),
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        });

        foreach (var s in shortages)
        {
            var label = string.IsNullOrWhiteSpace(s.Reference)
                ? s.Designation
                : $"{s.Reference} — {s.Designation}";

            var card = new StackPanel { Spacing = 4 };
            card.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeight.SemiBold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            });
            card.Children.Add(ValueLine(locale.Tf("Stock_ShortageDemande", s.Demande), DemandeBrush));
            card.Children.Add(ValueLine(locale.Tf("Stock_ShortageDisponible", s.Disponible), DisponibleBrush));
            card.Children.Add(ValueLine(locale.Tf("Stock_ShortageManque", s.Manquant), ManqueBrush));

            panel.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#08000000")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10),
                Child = card
            });
        }

        if (!block)
        {
            panel.Children.Add(new TextBlock
            {
                Text = locale.T("Stock_ShortageConfirm"),
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 0)
        };

        if (block)
        {
            var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 88 };
            ok.Click += (_, _) => w.Close();
            buttons.Children.Add(ok);
        }
        else
        {
            var no = new Button { Content = locale.T("Btn_No"), MinWidth = 88 };
            no.Click += (_, _) =>
            {
                confirmed = false;
                w.Close();
            };
            var yes = new Button { Content = locale.T("Btn_Yes"), IsDefault = true, MinWidth = 88 };
            yes.Click += (_, _) =>
            {
                confirmed = true;
                w.Close();
            };
            buttons.Children.Add(no);
            buttons.Children.Add(yes);
        }

        panel.Children.Add(buttons);

        var flow = locale.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        w.FlowDirection = flow;
        panel.FlowDirection = flow;
        w.Content = panel;

        if (owner != null)
            await w.ShowDialog(owner);
        else
            w.Show();

        return !block && confirmed;
    }

    private static TextBlock ValueLine(string text, IBrush foreground) => new()
    {
        Text = text,
        FontSize = 13,
        FontWeight = FontWeight.SemiBold,
        Foreground = foreground,
        TextWrapping = TextWrapping.Wrap
    };
}
