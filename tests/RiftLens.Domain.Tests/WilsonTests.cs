using RiftLens.Domain.Scoring;

namespace RiftLens.Domain.Tests;

public class WilsonTests
{
    [Fact]
    public void Petit_echantillon_a_une_borne_basse_prudente()
    {
        // 9/12 = 75 % mais la borne basse doit rester sous 50 % : pas de faux S+.
        var lower = Wilson.LowerBound(9, 12);
        Assert.InRange(lower, 0.46, 0.48);
    }

    [Fact]
    public void Grand_echantillon_converge_vers_le_winrate()
    {
        var (lower, upper) = Wilson.Interval(5300, 10000);
        Assert.InRange(lower, 0.52, 0.53);
        Assert.InRange(upper, 0.53, 0.54);
    }

    [Fact]
    public void Zero_partie_donne_intervalle_complet() => Assert.Equal((0d, 1d), Wilson.Interval(0, 0));

    [Fact]
    public void Plus_de_victoires_que_de_parties_est_refuse() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Wilson.Interval(5, 4));
}
