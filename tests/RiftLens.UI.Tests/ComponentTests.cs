using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RiftLens.Contracts;
using RiftLens.UI.Components;
using RiftLens.UI.Services;

namespace RiftLens.UI.Tests;

public sealed class ComponentTests : BunitContext
{
    public ComponentTests()
    {
        // Backend factice : manifeste vide, suffisant pour le rendu.
        var http = new HttpClient(new NotFoundHandler()) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new RiftLensApiClient(http));
        Services.AddSingleton<GameAssets>();
    }

    [Theory]
    [InlineData("S+", "rl-tier--splus")]
    [InlineData("a", "rl-tier--a")]
    public void TierBadge_applique_la_classe_du_tier(string tier, string css)
    {
        var cut = Render<TierBadge>(p => p.Add(x => x.Tier, tier));
        Assert.Contains(css, cut.Find("span").ClassList);
    }

    [Theory]
    [InlineData(0.44, "var(--rl-wr-low)")]
    [InlineData(0.50, "var(--rl-wr-mid)")]
    [InlineData(0.56, "var(--rl-wr-high)")]
    public void Echelle_de_couleur_des_winrates(double winrate, string expected) => Assert.Equal(expected, WinrateColor.For(winrate));

    [Fact]
    public void RankBadge_apex_sans_division()
    {
        var cut = Render<RankBadge>(p => p.Add(x => x.Tier, "GRANDMASTER").Add(x => x.Division, "I").Add(x => x.LeaguePoints, 412));
        Assert.Equal("GM", cut.Find(".rl-rank__emblem").TextContent);
        Assert.Equal("Grandmaster", cut.Find(".rl-rank__tier").TextContent);
    }

    [Fact]
    public void GoldDiffChart_affiche_un_etat_vide_sans_donnees()
    {
        var cut = Render<GoldDiffChart>(p => p.Add(x => x.Points, []));
        Assert.NotNull(cut.Find(".rl-chart-empty"));
    }

    [Fact]
    public void GoldDiffChart_affiche_l_infobulle_au_survol()
    {
        GoldDiffPointDto[] points = [new(0, 2500, 2500), new(1, 3000, 2800), new(2, 4200, 3600)];
        var cut = Render<GoldDiffChart>(p => p.Add(x => x.Points, points));

        cut.FindAll("rect[fill=transparent]")[2].MouseEnter();

        Assert.Contains("+600", cut.Find(".rl-gold__tip").TextContent.Replace(' ', ' ').Replace(' ', ' '), StringComparison.Ordinal);
    }

    [Fact]
    public void MatchRow_marque_les_victoires()
    {
        var match = new MatchSummaryDto("EUW1_1", 420, DateTimeOffset.Now.AddHours(-2), 1874, false, true, 103, 16, "Middle",
            11, 3, 12, 210, 31000, 25, 14000, 0.64, [3157, 3020, 0, 0, 0, 0, 3340], [4, 14], 8112, 8200, []);

        var cut = Render<MatchRow>(p => p.Add(x => x.Match, match));

        Assert.Contains("rl-match--win", cut.Find("article").ClassList);
        Assert.Contains("Victoire", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("7,67 KDA", cut.Markup.Replace('.', ','), StringComparison.Ordinal);
    }

    [Fact]
    public void Fmt_ago_et_duree()
    {
        var now = DateTimeOffset.Parse("2026-10-13T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("il y a 2 h", Fmt.Ago(now.AddHours(-2), now));
        Assert.Equal("31:14", Fmt.Duration(1874));
    }

    private sealed class NotFoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
