namespace RiftLens.Domain.Scoring;

/// <summary>Intervalle de score de Wilson pour une proportion binomiale.</summary>
public static class Wilson
{
    /// <summary>z pour un intervalle de confiance bilatéral à 95 %.</summary>
    public const double Z95 = 1.959963984540054;

    public static (double Lower, double Upper) Interval(int successes, int trials, double z = Z95)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(successes);
        ArgumentOutOfRangeException.ThrowIfLessThan(trials, successes);
        if (trials == 0)
        {
            return (0, 1);
        }

        double n = trials;
        var p = successes / n;
        var z2 = z * z;
        var denominator = 1 + (z2 / n);
        var centre = p + (z2 / (2 * n));
        var margin = z * Math.Sqrt((p * (1 - p) / n) + (z2 / (4 * n * n)));
        return ((centre - margin) / denominator, (centre + margin) / denominator);
    }

    public static double LowerBound(int successes, int trials, double z = Z95) => Interval(successes, trials, z).Lower;
}
