namespace RiftLens.Contracts;

/// <summary>Filtre d'elo : agrège les parties trouvées via les ladders de ce tier et au-dessus.</summary>
public static class EloFilters
{
    public const string MasterPlus = "master";
    public const string GrandmasterPlus = "grandmaster";
    public const string Challenger = "challenger";
}

public sealed record PatchListDto(IReadOnlyList<string> Patches);

public sealed record TierListDto(string Patch, string Elo, int TotalMatches, int MinGames, IReadOnlyList<TierListRowDto> Rows);

public sealed record TierListRowDto(
    int ChampionId,
    string Role,
    string Tier,
    double Score,
    int Games,
    int Wins,
    double Pickrate,
    double Banrate,
    bool SampleTooSmall)
{
    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;
}

public sealed record ChampionDetailDto(
    int ChampionId,
    string Patch,
    string Elo,
    string Role,
    int TotalMatches,
    TierListRowDto? Summary,
    IReadOnlyList<RoleShareDto> Roles,
    IReadOnlyList<BuildDto> Builds,
    IReadOnlyList<RunePageDto> RunePages,
    IReadOnlyList<MatchupDto> BestMatchups,
    IReadOnlyList<MatchupDto> WorstMatchups,
    int MinBuildGames,
    int MinMatchupGames)
{
    /// <summary>Objets de départ les plus joués (issus des timelines échantillonnées).</summary>
    public IReadOnlyList<BuildDto> StarterItems { get; init; } = [];

    /// <summary>Ordres d'achat des objets complets (bottes comprises), dans l'ordre.</summary>
    public IReadOnlyList<BuildDto> ItemOrders { get; init; } = [];

    public IReadOnlyList<SkillOrderDto> SkillOrders { get; init; } = [];

    /// <summary>Paires de sorts d'invocateur les plus jouées (sur tous les matchs).</summary>
    public IReadOnlyList<BuildDto> SummonerSpells { get; init; } = [];

    /// <summary>Parties avec timeline pour ce rôle (base des trois listes ci-dessus).</summary>
    public int TimelineGames { get; init; }

    public int MinSequenceGames { get; init; }
}

/// <param name="Sequence">Sorts montés niveau par niveau, ex. « QWEQQRQEQEREEWW ».</param>
/// <param name="Priority">Ordre de maximisation, ex. « Q > E > W ».</param>
public sealed record SkillOrderDto(string Sequence, string Priority, int Games, int Wins)
{
    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;
}

public sealed record BuildDto(IReadOnlyList<int> Items, int Games, int Wins)
{
    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;
}

public sealed record RunePageDto(int PrimaryStyle, int SubStyle, IReadOnlyList<int> Perks, int Games, int Wins)
{
    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;
}

public sealed record MatchupDto(int OpponentId, int Games, int Wins)
{
    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;
}

/// <summary>Tous les matchups observés d'un champion dans un rôle (y compris faibles échantillons, à signaler).</summary>
public sealed record MatchupListDto(int ChampionId, string Role, string Patch, string Elo, int MinGames, IReadOnlyList<MatchupDto> Matchups);
