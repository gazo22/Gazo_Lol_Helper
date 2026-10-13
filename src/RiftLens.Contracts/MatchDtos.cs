namespace RiftLens.Contracts;

/// <summary>Une ligne d'historique, vue depuis le joueur consulté.</summary>
public sealed record MatchSummaryDto(
    string MatchId,
    int QueueId,
    DateTimeOffset GameStart,
    int DurationSeconds,
    bool IsRemake,
    bool Win,
    int ChampionId,
    int ChampLevel,
    string Role,
    int Kills,
    int Deaths,
    int Assists,
    int Cs,
    int Damage,
    int VisionScore,
    int Gold,
    double KillParticipation,
    IReadOnlyList<int> Items,
    IReadOnlyList<int> Summoners,
    int Keystone,
    int SubStyle,
    IReadOnlyList<MatchPlayerLiteDto> Players)
{
    public double CsPerMinute => DurationSeconds == 0 ? 0 : Cs / (DurationSeconds / 60.0);

    public double Kda => Deaths == 0 ? Kills + Assists : (double)(Kills + Assists) / Deaths;
}

public sealed record MatchPlayerLiteDto(int TeamId, int ChampionId, string? GameName, string? TagLine);

public sealed record MatchDetailDto(
    string MatchId,
    int QueueId,
    string GameVersion,
    DateTimeOffset GameStart,
    int DurationSeconds,
    bool IsRemake,
    IReadOnlyList<TeamDetailDto> Teams);

public sealed record TeamDetailDto(int TeamId, bool Win, int Kills, int Gold, IReadOnlyList<int> Bans, IReadOnlyList<MatchPlayerDto> Players);

public sealed record MatchPlayerDto(
    int ParticipantId,
    string Puuid,
    string? GameName,
    string? TagLine,
    int ChampionId,
    int ChampLevel,
    string Role,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int Cs,
    int Gold,
    int Damage,
    int DamageTaken,
    int VisionScore,
    IReadOnlyList<int> Items,
    IReadOnlyList<int> Summoners,
    IReadOnlyList<int> Perks,
    int PrimaryStyle,
    int SubStyle);

/// <summary>Or cumulé par équipe à chaque minute ; <c>Diff</c> = bleu − rouge.</summary>
public sealed record GoldDiffPointDto(int Minute, int BlueGold, int RedGold)
{
    public int Diff => BlueGold - RedGold;
}

public static class Queues
{
    public const int RankedSolo = 420;
    public const int RankedFlex = 440;

    public static string Name(int queueId) => queueId switch
    {
        420 => "Classée solo/duo",
        440 => "Classée flexible",
        400 => "Normale (draft)",
        430 => "Normale (aveugle)",
        490 => "Partie rapide",
        450 => "ARAM",
        1700 or 1710 => "Arena",
        _ => "Autre mode",
    };
}
