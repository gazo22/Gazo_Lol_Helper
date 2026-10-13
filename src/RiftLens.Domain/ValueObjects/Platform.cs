namespace RiftLens.Domain.ValueObjects;

/// <summary>Plateformes (serveurs) Riot et leur routage régional associé.</summary>
public enum Platform
{
    Euw1,
    Eun1,
    Tr1,
    Ru,
    Na1,
    Br1,
    La1,
    La2,
    Kr,
    Jp1,
    Oc1,
}

public enum RegionalRoute
{
    Europe,
    Americas,
    Asia,
    Sea,
}

public static class PlatformExtensions
{
    public static string ToHost(this Platform platform) => platform.ToString().ToLowerInvariant();

    public static RegionalRoute ToRegional(this Platform platform) => platform switch
    {
        Platform.Euw1 or Platform.Eun1 or Platform.Tr1 or Platform.Ru => RegionalRoute.Europe,
        Platform.Na1 or Platform.Br1 or Platform.La1 or Platform.La2 => RegionalRoute.Americas,
        Platform.Kr or Platform.Jp1 => RegionalRoute.Asia,
        Platform.Oc1 => RegionalRoute.Sea,
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null),
    };

    public static string ToHost(this RegionalRoute route) => route.ToString().ToLowerInvariant();
}

public static class MatchIds
{
    /// <summary>Déduit la plateforme d'un identifiant Match-V5 (<c>EUW1_7123456789</c> → Euw1).</summary>
    public static bool TryGetPlatform(string matchId, out Platform platform)
    {
        platform = default;
        var underscore = matchId.IndexOf('_', StringComparison.Ordinal);
        return underscore > 0 && Enum.TryParse(matchId[..underscore], ignoreCase: true, out platform);
    }
}
