namespace RiftLens.Infrastructure.Stats;

public sealed class StatsOptions
{
    public const string SectionName = "Stats";

    /// <summary>Parties minimales pour qu'un champion/rôle entre dans le calcul des percentiles de tier.</summary>
    public int MinGames { get; set; } = 150;

    /// <summary>Parties minimales pour afficher un build ou une page de runes.</summary>
    public int MinBuildGames { get; set; } = 30;

    /// <summary>Parties minimales pour afficher une séquence (départ, ordre d'achat, ordre des sorts) — issues d'un échantillon.</summary>
    public int MinSequenceGames { get; set; } = 10;

    /// <summary>Parties minimales pour afficher un matchup.</summary>
    public int MinMatchupGames { get; set; } = 20;

    /// <summary>Pickrate minimal pour apparaître dans la tier list (évite les rôles « off-meta » anecdotiques).</summary>
    public double MinPickrate { get; set; } = 0.005;
}
