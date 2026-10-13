using RiftLens.Infrastructure.StaticData;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.StaticData;

public class DataDragonParseTests
{
    [Fact]
    public void Indexe_les_donnees_par_identifiant_numerique()
    {
        using var champions = Fixture.Doc("ddragon/champion.json");
        using var items = Fixture.Doc("ddragon/item.json");
        using var runes = Fixture.Doc("ddragon/runesReforged.json");
        using var spells = Fixture.Doc("ddragon/summoner.json");

        var manifest = DataDragonService.Parse("15.20.1", champions, items, runes, spells);

        Assert.Equal("MonkeyKing", manifest.Champions[62].Key);
        Assert.Equal("Wukong", manifest.Champions[62].Name);
        Assert.Equal("Sablier de Zhonya", manifest.Items[3157]);
        Assert.Equal("Électrocution", manifest.Runes[8112].Name);
        Assert.Equal("perk-images/Styles/7200_Domination.png", manifest.Runes[8100].Icon);
        Assert.Equal("SummonerFlash", manifest.SummonerSpells[4].Key);
    }
}
