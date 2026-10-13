using RiftLens.Domain.ValueObjects;

namespace RiftLens.Infrastructure.Collection;

public sealed class CollectorOptions
{
    public const string SectionName = "Collector";

    public Platform Platform { get; set; } = Platform.Euw1;

    public List<RankTier> Tiers { get; } = [RankTier.Challenger, RankTier.Grandmaster, RankTier.Master];

    /// <summary>Fréquence de rafraîchissement des ladders.</summary>
    public TimeSpan SeedInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>Joueurs dont on parcourt l'historique à chaque cycle.</summary>
    public int PlayersPerCycle { get; set; } = 10;

    public int MatchesPerPlayer { get; set; } = 20;

    /// <summary>Ne récupère que les matchs des N derniers jours.</summary>
    public int LookbackDays { get; set; } = 7;

    public int FetchBatchSize { get; set; } = 25;

    /// <summary>Au-delà de ce nombre de matchs en attente, on ne cherche pas de nouveaux ids.</summary>
    public int MaxPending { get; set; } = 500;

    public int MaxAttempts { get; set; } = 3;

    public TimeSpan AggregateInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Patchs agrégés : le plus récent et le(s) précédent(s).</summary>
    public int PatchesToAggregate { get; set; } = 2;

    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Pause quand la clé Riot est refusée (clé de dev expirée).</summary>
    public TimeSpan AuthFailureDelay { get; set; } = TimeSpan.FromMinutes(15);
}
