namespace RiftLens.Contracts;

public sealed record ProfileDto(
    string Puuid,
    string GameName,
    string TagLine,
    string Platform,
    int ProfileIconId,
    long SummonerLevel,
    RankDto? Solo,
    RankDto? Flex,
    IReadOnlyList<LpPointDto> LpHistory,
    ProfileStatsDto Stats,
    IReadOnlyList<ChampionPerformanceDto> TopChampions,
    IReadOnlyList<RoleShareDto> Roles,
    IReadOnlyList<MasteryDto> Masteries,
    DateTimeOffset LastRefreshedAt);

/// <summary>Rang dans une file. <c>Tier</c> en majuscules Riot (GOLD, MASTER…), <c>Division</c> I-IV.</summary>
public sealed record RankDto(string Queue, string Tier, string Division, int LeaguePoints, int Wins, int Losses, bool HotStreak)
{
    public int Games => Wins + Losses;

    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;
}

public sealed record LpPointDto(DateTimeOffset CapturedAt, string Tier, string Division, int LeaguePoints, int LadderScore);

/// <summary>Moyennes sur les parties stockées (Faille de l'invocateur).</summary>
public sealed record ProfileStatsDto(
    int Games,
    int Wins,
    double Kills,
    double Deaths,
    double Assists,
    double CsPerMinute,
    double VisionPerMinute,
    double DamagePerMinute,
    double KillParticipation)
{
    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;

    public double Kda => Deaths == 0 ? Kills + Assists : (Kills + Assists) / Deaths;

    public static ProfileStatsDto Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public sealed record ChampionPerformanceDto(int ChampionId, int Games, int Wins, double Kda, double CsPerMinute)
{
    public double Winrate => Games == 0 ? 0 : (double)Wins / Games;
}

public sealed record RoleShareDto(string Role, int Games, int Wins);

public sealed record MasteryDto(int ChampionId, int Level, int Points);
