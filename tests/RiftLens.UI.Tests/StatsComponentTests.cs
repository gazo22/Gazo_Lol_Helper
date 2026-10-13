using Microsoft.Extensions.DependencyInjection;
using RiftLens.UI.Components;
using RiftLens.UI.Services;

namespace RiftLens.UI.Tests;

public sealed class StatsComponentTests : BunitContext
{
    [Fact]
    public void SortHeader_indique_le_sens_de_tri()
    {
        string? clicked = null;
        var cut = Render<SortHeader>(p => p
            .Add(x => x.Label, "Winrate").Add(x => x.Column, "winrate")
            .Add(x => x.Sort, new SortState("winrate", true))
            .Add(x => x.OnSort, (string c) => clicked = c));

        Assert.Equal("descending", cut.Find("th").GetAttribute("aria-sort"));
        cut.Find("th").Click();
        Assert.Equal("winrate", clicked);
    }

    [Fact]
    public void StatFilters_emet_le_role_choisi()
    {
        string? role = "none";
        var cut = Render<StatFilters>(p => p.Add(x => x.Patches, ["15.20", "15.19"]).Add(x => x.RoleChanged, (string? r) => role = r));

        cut.FindAll(".rl-segmented button")[3].Click(); // Tous, Top, Jungle, Middle
        Assert.Equal("Middle", role);
        Assert.Equal(2, cut.FindAll("option").Count - 3); // 2 patchs + 3 elos
    }

    [Theory]
    [InlineData("Kaï'Sa", "kaisa")]
    [InlineData("Nunu et Willump", "nunuetwillump")]
    [InlineData("Bel'Veth", "belveth")]
    public void Recherche_champion_insensible_aux_accents(string name, string expected) =>
        Assert.Equal(expected, ChampionPaletteSource.Normalize(name));
}
