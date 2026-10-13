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
    int MinMatchupGames);

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
