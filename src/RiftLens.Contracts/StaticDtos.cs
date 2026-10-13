namespace RiftLens.Contracts;

/// <summary>Données statiques Data Dragon nécessaires à l'affichage (noms + clés d'images).</summary>
public sealed record StaticManifestDto(
    string Version,
    IReadOnlyDictionary<int, ChampionStaticDto> Champions,
    IReadOnlyDictionary<int, string> Items,
    IReadOnlyDictionary<int, RuneStaticDto> Runes,
    IReadOnlyDictionary<int, SummonerSpellStaticDto> SummonerSpells)
{
    public static StaticManifestDto Empty { get; } = new("", new Dictionary<int, ChampionStaticDto>(), new Dictionary<int, string>(), new Dictionary<int, RuneStaticDto>(), new Dictionary<int, SummonerSpellStaticDto>());
}

/// <summary><c>Key</c> = identifiant texte Data Dragon (ex. "MonkeyKing"), utilisé dans les URLs d'images.</summary>
public sealed record ChampionStaticDto(string Key, string Name);

/// <summary><c>Icon</c> = chemin relatif à <c>cdn/img/</c>.</summary>
public sealed record RuneStaticDto(string Name, string Icon);

public sealed record SummonerSpellStaticDto(string Key, string Name);
