using System.Globalization;

namespace RiftLens.UI.Components;

/// <summary>Formatage homogène des statistiques (culture courante, chiffres tabulaires côté CSS).</summary>
public static class Fmt
{
    private static CultureInfo C => CultureInfo.CurrentCulture;

    public static string Percent(double ratio, int decimals = 1) => (ratio * 100).ToString("F" + decimals, C) + " %";

    public static string Dec(double value, int decimals = 1) => value.ToString("F" + decimals, C);

    public static string Num(int value) => value.ToString("N0", C);

    /// <summary>12 345 → « 12,3 k ».</summary>
    public static string Compact(int value) => value >= 1000 ? (value / 1000.0).ToString("0.#", C) + " k" : value.ToString(C);

    public static string Duration(int seconds) => $"{seconds / 60}:{seconds % 60:00}";

    public static string Kda(double kda) => kda >= 99 ? "Parfait" : kda.ToString("F2", C);

    public static string Ago(DateTimeOffset when, DateTimeOffset now)
    {
        var span = now - when;
        return span switch
        {
            { TotalMinutes: < 1 } => "à l'instant",
            { TotalHours: < 1 } => $"il y a {(int)span.TotalMinutes} min",
            { TotalDays: < 1 } => $"il y a {(int)span.TotalHours} h",
            { TotalDays: < 30 } => $"il y a {(int)span.TotalDays} j",
            _ => when.ToLocalTime().ToString("d MMM yyyy", C),
        };
    }

    public static string RoleLabel(string role) => role switch
    {
        "Top" => "Top",
        "Jungle" => "Jungle",
        "Middle" => "Mid",
        "Bottom" => "ADC",
        "Support" => "Support",
        _ => "—",
    };
}
