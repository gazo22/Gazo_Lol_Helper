using RiftLens.Domain.ValueObjects;

namespace RiftLens.Domain.Entities;

// Agrégats recalculés par (patch, bucket d'elo). Le bucket est le tier du joueur ladder d'où provient le match.

public sealed class PatchTotals
{
    public required string Patch { get; init; }
    public RankTier Bucket { get; init; }
    public int Matches { get; set; }
    public DateTimeOffset ComputedAt { get; set; }
}

public sealed class ChampionStat
{
    public required string Patch { get; init; }
    public RankTier Bucket { get; init; }
    public int ChampionId { get; init; }
    public Role Role { get; init; }
    public int Games { get; set; }
    public int Wins { get; set; }
}

public sealed class ChampionBanStat
{
    public required string Patch { get; init; }
    public RankTier Bucket { get; init; }
    public int ChampionId { get; init; }
    public int Bans { get; set; }
}

public sealed class MatchupStat
{
    public required string Patch { get; init; }
    public RankTier Bucket { get; init; }
    public Role Role { get; init; }
    public int ChampionId { get; init; }
    public int OpponentId { get; init; }
    public int Games { get; set; }
    public int Wins { get; set; }
}

/// <summary>Core build : les 3 premiers objets légendaires (hors bottes), ex. "3157-4645-3089".</summary>
public sealed class BuildStat
{
    public required string Patch { get; init; }
    public RankTier Bucket { get; init; }
    public int ChampionId { get; init; }
    public Role Role { get; init; }
    public required string ItemsKey { get; init; }
    public int Games { get; set; }
    public int Wins { get; set; }
}

/// <summary>Page de runes : "primaire-secondaire:r1-r2-r3-r4-r5-r6-f1-f2-f3".</summary>
public sealed class RuneStat
{
    public required string Patch { get; init; }
    public RankTier Bucket { get; init; }
    public int ChampionId { get; init; }
    public Role Role { get; init; }
    public required string PerksKey { get; init; }
    public int Games { get; set; }
    public int Wins { get; set; }
}

public enum SequenceKind
{
    /// <summary>Objets achetés avant 1 min 30, triés : « 1055-2003-2003 ».</summary>
    StarterItems,
    /// <summary>Objets complets (légendaires + bottes) dans l'ordre d'achat : « 3006-3031-3094-3036 ».</summary>
    ItemOrder,
    /// <summary>Montée des sorts sur 15 niveaux : « QWEQQRQEQEREEWW ».</summary>
    SkillOrder,
    /// <summary>Paire de sorts d'invocateur triée : « 4-14 » (tous les matchs, sans timeline).</summary>
    SummonerSpells,
}

/// <summary>Séquences issues des timelines (échantillon des matchs collectés).</summary>
public sealed class SequenceStat
{
    public required string Patch { get; init; }
    public RankTier Bucket { get; init; }
    public int ChampionId { get; init; }
    public Role Role { get; init; }
    public SequenceKind Kind { get; init; }
    public required string Key { get; init; }
    public int Games { get; set; }
    public int Wins { get; set; }
}
