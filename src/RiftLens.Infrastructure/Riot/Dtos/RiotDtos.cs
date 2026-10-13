using System.Text.Json.Serialization;

namespace RiftLens.Infrastructure.Riot.Dtos;

// Account-V1
public sealed record AccountDto(string Puuid, string? GameName, string? TagLine);

// Summoner-V4 (by-puuid ; les champs id/accountId ont été retirés par Riot)
public sealed record SummonerDto(string Puuid, int ProfileIconId, long RevisionDate, long SummonerLevel);

// League-V4
public sealed record LeagueEntryDto(
    string? LeagueId,
    string QueueType,
    string Tier,
    string Rank,
    string Puuid,
    int LeaguePoints,
    int Wins,
    int Losses,
    bool HotStreak,
    bool Veteran,
    bool FreshBlood,
    bool Inactive);

// Champion-Mastery-V4
public sealed record ChampionMasteryDto(string Puuid, int ChampionId, int ChampionLevel, int ChampionPoints, long LastPlayTime);

// Match-V5
public sealed record MatchDto(MatchMetadataDto Metadata, MatchInfoDto Info);

public sealed record MatchMetadataDto(string MatchId, IReadOnlyList<string> Participants);

public sealed record MatchInfoDto(
    long GameCreation,
    long GameDuration,
    long GameStartTimestamp,
    long? GameEndTimestamp,
    string GameVersion,
    int QueueId,
    int MapId,
    string? PlatformId,
    IReadOnlyList<ParticipantDto> Participants,
    IReadOnlyList<TeamDto> Teams);

public sealed record ParticipantDto
{
    public int ParticipantId { get; init; }
    public required string Puuid { get; init; }
    public string? RiotIdGameName { get; init; }
    public string? RiotIdTagline { get; init; }
    public int TeamId { get; init; }
    public bool Win { get; init; }
    public int ChampionId { get; init; }
    public string? ChampionName { get; init; }
    public int ChampLevel { get; init; }
    public string? TeamPosition { get; init; }
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }
    public int TotalMinionsKilled { get; init; }
    public int NeutralMinionsKilled { get; init; }
    public int GoldEarned { get; init; }
    public int TotalDamageDealtToChampions { get; init; }
    public int TotalDamageTaken { get; init; }
    public int VisionScore { get; init; }
    public int WardsPlaced { get; init; }
    public int Item0 { get; init; }
    public int Item1 { get; init; }
    public int Item2 { get; init; }
    public int Item3 { get; init; }
    public int Item4 { get; init; }
    public int Item5 { get; init; }
    public int Item6 { get; init; }
    public int Summoner1Id { get; init; }
    public int Summoner2Id { get; init; }
    public bool GameEndedInEarlySurrender { get; init; }
    public PerksDto? Perks { get; init; }
}

public sealed record PerksDto(PerkStatsDto StatPerks, IReadOnlyList<PerkStyleDto> Styles);

public sealed record PerkStatsDto(int Defense, int Flex, int Offense);

public sealed record PerkStyleDto(string Description, int Style, IReadOnlyList<PerkSelectionDto> Selections);

public sealed record PerkSelectionDto(int Perk);

public sealed record TeamDto(int TeamId, bool Win, IReadOnlyList<BanDto> Bans);

public sealed record BanDto(int ChampionId, int PickTurn);

// Match-V5 timeline
public sealed record TimelineDto(TimelineInfoDto Info);

public sealed record TimelineInfoDto(long FrameInterval, IReadOnlyList<TimelineFrameDto> Frames);

public sealed record TimelineFrameDto(long Timestamp, IReadOnlyDictionary<string, ParticipantFrameDto> ParticipantFrames);

public sealed record ParticipantFrameDto(int ParticipantId, int TotalGold, int Level, int MinionsKilled, int JungleMinionsKilled, int Xp);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AccountDto))]
[JsonSerializable(typeof(SummonerDto))]
[JsonSerializable(typeof(IReadOnlyList<LeagueEntryDto>))]
[JsonSerializable(typeof(IReadOnlyList<ChampionMasteryDto>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(MatchDto))]
[JsonSerializable(typeof(TimelineDto))]
internal sealed partial class RiotJsonContext : JsonSerializerContext;
