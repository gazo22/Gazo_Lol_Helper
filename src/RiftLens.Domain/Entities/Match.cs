namespace RiftLens.Domain.Entities;

/// <summary>Match Match-V5 stocké une seule fois (clé = matchId, ex. <c>EUW1_1234567890</c>).</summary>
public sealed class Match
{
    public required string MatchId { get; init; }
    public int QueueId { get; init; }
    public required string GameVersion { get; init; }
    public int PatchMajor { get; init; }
    public int PatchMinor { get; init; }
    public DateTimeOffset GameStart { get; init; }
    public TimeSpan Duration { get; init; }
    public bool IsRemake { get; init; }

    /// <summary>Timeline brute (JSON compressé gzip), chargée à la demande.</summary>
    public byte[]? TimelineGzip { get; set; }

    public List<MatchParticipant> Participants { get; } = [];
    public List<MatchBan> Bans { get; } = [];
}

public sealed class MatchBan
{
    public required string MatchId { get; init; }
    public int TeamId { get; init; }
    public int PickTurn { get; init; }
    public int ChampionId { get; init; }
}
