using System.Text;
using GestionCommerciale.Shared.Services;

namespace GestionCommerciale.Modules.Stock.Services;

public static class StockShortageDialog
{
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

        var message = FormatMessage(locale, shortages);
        if (block)
        {
            await dialog.ShowErrorAsync(title, message, cancellationToken);
            return false;
        }

        return await dialog.ConfirmAsync(
            title,
            message + Environment.NewLine + Environment.NewLine + locale.T("Stock_ShortageConfirm"),
            cancellationToken);
    }

    public static string FormatMessage(ILocaleService locale, IReadOnlyList<StockShortageItem> shortages)
    {
        var location = shortages[0].LocationName;
        var sb = new StringBuilder();
        sb.AppendLine(locale.Tf("Stock_ShortageHeader", location));
        sb.AppendLine();
        foreach (var s in shortages)
        {
            var label = string.IsNullOrWhiteSpace(s.Reference)
                ? s.Designation
                : $"{s.Reference} — {s.Designation}";
            sb.AppendLine(locale.Tf(
                "Stock_ShortageLine",
                label,
                s.Demande,
                s.Disponible,
                s.Manquant));
        }

        return sb.ToString().TrimEnd();
    }
}
