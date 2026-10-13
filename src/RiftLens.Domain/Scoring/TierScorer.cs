namespace RiftLens.Domain.Scoring;

/// <summary>
/// Score et tier d'un champion dans un rôle. Voir <c>docs/tier-score.md</c>.
/// <para>score = borne basse de Wilson (95 %) du winrate + 0,02 × log2(1 + pickrate / 1 %)</para>
/// Les tiers sont attribués par percentile au sein d'un même rôle.
/// </summary>
public static class TierScorer
{
    public const double PopularityWeight = 0.02;
    public const double PickrateUnit = 0.01;

    /// <summary>Bornes supérieures de percentile (exclusives) : S+ 5 %, S 15 %, A 30 %, B 30 %, C le reste.</summary>
    private static readonly (double Percentile, TierGrade Grade)[] Bands =
    [
        (0.05, TierGrade.SPlus),
        (0.20, TierGrade.S),
        (0.50, TierGrade.A),
        (0.80, TierGrade.B),
    ];

    public static double Score(int wins, int games, double pickrate) =>
        Wilson.LowerBound(wins, games) + (PopularityWeight * Math.Log2(1 + (Math.Max(0, pickrate) / PickrateUnit)));

    /// <summary>
    /// Attribue les tiers d'un rôle. Les entrées sous <paramref name="minGames"/> ne participent pas aux percentiles
    /// et sont plafonnées à B : un champion à 12 parties ne peut pas être S+.
    /// </summary>
    public static IReadOnlyDictionary<TKey, TierResult> Grade<TKey>(IEnumerable<TierInput<TKey>> inputs, int minGames)
        where TKey : notnull
    {
        var scored = inputs.Select(i => (i.Key, i.Games, Score: Score(i.Wins, i.Games, i.Pickrate))).ToList();
        var eligible = scored.Where(s => s.Games >= minGames).OrderByDescending(s => s.Score).ToList();
        var result = new Dictionary<TKey, TierResult>();

        // Seuils de score issus des percentiles de la population éligible.
        var cutoffs = Bands.Select(b => (b.Grade, MinScore: eligible.Count == 0
            ? double.MaxValue
            : eligible[Math.Min(eligible.Count - 1, Math.Max(0, (int)Math.Ceiling(b.Percentile * eligible.Count) - 1))].Score)).ToArray();

        for (var i = 0; i < eligible.Count; i++)
        {
            var percentile = (double)i / eligible.Count;
            var grade = Bands.FirstOrDefault(b => percentile < b.Percentile, (1, TierGrade.C)).Grade;
            result[eligible[i].Key] = new TierResult(grade, eligible[i].Score, SampleTooSmall: false);
        }

        foreach (var low in scored.Where(s => s.Games < minGames))
        {
            var grade = cutoffs.FirstOrDefault(c => low.Score >= c.MinScore, (TierGrade.C, 0)).Grade;
            result[low.Key] = new TierResult(grade > TierGrade.B ? TierGrade.B : grade, low.Score, SampleTooSmall: true);
        }

        return result;
    }

    public static string Label(TierGrade grade) => grade == TierGrade.SPlus ? "S+" : grade.ToString();
}

public sealed record TierInput<TKey>(TKey Key, int Games, int Wins, double Pickrate);

public sealed record TierResult(TierGrade Grade, double Score, bool SampleTooSmall);
