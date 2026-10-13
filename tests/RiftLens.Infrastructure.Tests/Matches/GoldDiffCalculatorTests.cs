using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.Matches;

public class GoldDiffCalculatorTests
{
    [Fact]
    public void Calcule_l_ecart_par_minute()
    {
        var teams = Enumerable.Range(1, 10).ToDictionary(i => i, i => i <= 5 ? 100 : 200);

        var points = GoldDiffCalculator.Compute(Fixture.Timeline(), teams);

        Assert.Equal(32, points.Count);
        Assert.Equal(Enumerable.Range(0, 32), points.Select(p => p.Minute));
        // Frame 10 : bleu = 5*(500+3800) + 7*(1+..+5) ; rouge = 5*(500+3500) + 7*(6+..+10)
        var p10 = points[10];
        Assert.Equal((5 * 4300) + (7 * 15), p10.BlueGold);
        Assert.Equal((5 * 4000) + (7 * 40), p10.RedGold);
        Assert.Equal(p10.BlueGold - p10.RedGold, p10.Diff);
    }
}
