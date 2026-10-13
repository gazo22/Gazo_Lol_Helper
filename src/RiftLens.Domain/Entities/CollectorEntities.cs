using RiftLens.Domain.ValueObjects;

namespace RiftLens.Domain.Entities;

/// <summary>Joueur high-elo dont le Collector parcourt l'historique.</summary>
public sealed class CollectorPlayer
{
    public required string Puuid { get; init; }
    public Platform Platform { get; set; }
    public RankTier Tier { get; set; }
    public int LeaguePoints { get; set; }
    public DateTimeOffset SeenAt { get; set; }
    public DateTimeOffset? LastCrawledAt { get; set; }
}

public enum QueueStatus
{
    Pending,
    Done,
    Failed,
}

/// <summary>File persistante des matchs à télécharger : c'est elle qui permet la reprise après interruption.</summary>
public sealed class QueuedMatch
{
    public required string MatchId { get; init; }
    public RankTier SourceTier { get; init; }
    public QueueStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset EnqueuedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
