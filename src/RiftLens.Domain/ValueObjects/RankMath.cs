namespace RiftLens.Domain.ValueObjects;

public static class RankMath
{
    /// <summary>
    /// Score linéaire pour tracer l'évolution : 400 points par tier (100 par division) + LP.
    /// Master et au-delà partagent la même base, seuls les LP départagent.
    /// </summary>
    public static int LadderScore(RankTier tier, int division, int leaguePoints)
    {
        if (tier == RankTier.Unranked)
        {
            return 0;
        }

        var tierBase = ((int)tier - 1) * 400;
        return tier >= RankTier.Master
            ? ((int)RankTier.Master - 1) * 400 + leaguePoints
            : tierBase + ((4 - Math.Clamp(division, 1, 4)) * 100) + leaguePoints;
    }

    public static RankTier ParseTier(string? tier) =>
        Enum.TryParse<RankTier>(tier, ignoreCase: true, out var t) ? t : RankTier.Unranked;

    /// <summary>"I" → 1 … "IV" → 4 ; 0 si absent.</summary>
    public static int ParseDivision(string? division) => division switch
    {
        "I" => 1,
        "II" => 2,
        "III" => 3,
        "IV" => 4,
        _ => 0,
    };

    public static string FormatDivision(int division) => division switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        _ => string.Empty,
    };
}
