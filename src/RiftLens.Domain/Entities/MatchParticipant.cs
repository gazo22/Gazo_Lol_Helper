using RiftLens.Domain.ValueObjects;

namespace RiftLens.Domain.Entities;

public sealed class MatchParticipant
{
    public required string MatchId { get; init; }
    public int ParticipantId { get; init; }
    public required string Puuid { get; init; }
    public string? RiotIdGameName { get; init; }
    public string? RiotIdTagline { get; init; }
    public int TeamId { get; init; }
    public bool Win { get; init; }
    public int ChampionId { get; init; }
    public Role Role { get; init; }
    public int ChampLevel { get; init; }

    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }
    public int TotalMinionsKilled { get; init; }
    public int NeutralMinionsKilled { get; init; }
    public int GoldEarned { get; init; }
    public int TotalDamageDealtToChampions { get; init; }
    public int DamageTaken { get; init; }
    public int VisionScore { get; init; }
    public int WardsPlaced { get; init; }

    /// <summary>Items finaux (slots 0-5) + trinket (slot 6).</summary>
    public int[] Items { get; init; } = new int[7];
    /// <summary>Sorts d'invocateur (D, F).</summary>
    public int[] Summoners { get; init; } = new int[2];
    public int PrimaryStyle { get; init; }
    public int SubStyle { get; init; }
    /// <summary>Les 6 runes sélectionnées + 3 fragments de stats.</summary>
    public int[] Perks { get; init; } = new int[9];

    public int TotalCs => TotalMinionsKilled + NeutralMinionsKilled;
}
