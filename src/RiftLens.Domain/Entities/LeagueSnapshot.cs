using RiftLens.Domain.ValueObjects;

namespace RiftLens.Domain.Entities;

/// <summary>Rang observé à un instant donné (sert à tracer l'évolution des LP).</summary>
public sealed class LeagueSnapshot
{
    public long Id { get; set; }
    public required string Puuid { get; init; }
    public QueueType Queue { get; init; }
    public RankTier Tier { get; init; }
    public int Division { get; init; }
    public int LeaguePoints { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
}
