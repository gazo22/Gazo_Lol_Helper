using RiftLens.Domain.Scoring;

namespace RiftLens.Domain.Tests;

public class TierScorerTests
{
    [Fact]
    public void Un_gros_winrate_sur_12_parties_ne_bat_pas_un_bon_winrate_sur_1000()
    {
        Assert.True(TierScorer.Score(530, 1000, 0.05) > TierScorer.Score(9, 12, 0.05));
    }

    [Fact]
    public void A_winrate_egal_la_popularite_departage()
    {
        Assert.True(TierScorer.Score(520, 1000, 0.12) > TierScorer.Score(520, 1000, 0.02));
    }

    [Fact]
    public void Les_percentiles_donnent_la_repartition_attendue()
    {
        // 20 champions éligibles, winrates croissants de 44 % à 56 %.
        var inputs = Enumerable.Range(0, 20).Select(i => new TierInput<int>(i, 1000, 440 + (i * 6), 0.05)).ToList();

        var grades = TierScorer.Grade(inputs, minGames: 150);

        Assert.Equal(TierGrade.SPlus, grades[19].Grade); // top 5 % = 1 champion
        Assert.Equal(3, grades.Values.Count(g => g.Grade == TierGrade.S));
        Assert.Equal(6, grades.Values.Count(g => g.Grade == TierGrade.A));
        Assert.Equal(6, grades.Values.Count(g => g.Grade == TierGrade.B));
        Assert.Equal(4, grades.Values.Count(g => g.Grade == TierGrade.C));
        Assert.Equal(TierGrade.C, grades[0].Grade);
    }

    [Fact]
    public void Un_petit_echantillon_est_plafonne_a_B_et_signale()
    {
        var inputs = Enumerable.Range(0, 10).Select(i => new TierInput<string>($"c{i}", 1000, 480 + (i * 5), 0.05)).ToList();
        inputs.Add(new TierInput<string>("rare", 12, 12, 0.001)); // 100 % sur 12 parties

        var grades = TierScorer.Grade(inputs, minGames: 150);

        Assert.True(grades["rare"].SampleTooSmall);
        Assert.True(grades["rare"].Grade <= TierGrade.B);
        Assert.False(grades["c9"].SampleTooSmall);
    }

    [Fact]
    public void Labels() => Assert.Equal(["S+", "S", "C"], [TierScorer.Label(TierGrade.SPlus), TierScorer.Label(TierGrade.S), TierScorer.Label(TierGrade.C)]);
}
