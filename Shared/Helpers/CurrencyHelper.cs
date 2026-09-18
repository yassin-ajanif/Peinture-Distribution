using System.Globalization;
using GestionCommerciale.Shared.Database;

namespace GestionCommerciale.Shared.Helpers;

public static class CurrencyHelper
{
    public const string DefaultCode = "DH";

    public static string Format(decimal amount, string? currencyCode = null)
    {
        var code = string.IsNullOrWhiteSpace(currencyCode) ? DefaultCode : currencyCode.Trim();
        var c = CultureInfo.GetCultureInfo("fr-FR");
        return amount.ToString("N2", c) + " " + code;
    }

    public static string FromSettings(AppSettingsRow cfg) =>
        string.IsNullOrWhiteSpace(cfg.Devise) ? DefaultCode : cfg.Devise.Trim();
}
