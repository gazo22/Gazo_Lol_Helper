namespace RiftLens.Domain.Entities;

/// <summary>Compte Riot connu (Account-V1), indexé par PUUID.</summary>
public sealed class Player
{
    public required string Puuid { get; init; }
    public required string GameName { get; set; }
    public required string TagLine { get; set; }
    public ValueObjects.Platform Platform { get; set; }
    public int ProfileIconId { get; set; }
    public long SummonerLevel { get; set; }
    public DateTimeOffset LastRefreshedAt { get; set; }

    public List<LeagueSnapshot> LeagueSnapshots { get; } = [];
}
