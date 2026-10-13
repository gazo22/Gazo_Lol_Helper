using RiftLens.Infrastructure.StaticData;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.StaticData;

public class ItemCategoriesTests
{
    private static ItemCategories Load()
    {
        using var doc = Fixture.Doc("ddragon/item.json");
        return DataDragonService.ParseItemCategories(doc);
    }

    [Fact]
    public void Classe_legendaires_et_bottes()
    {
        var c = Load();
        Assert.Equal(new HashSet<int> { 3157, 3071, 4645, 3089 }, c.Legendary.ToHashSet());
        Assert.Contains(3020, c.Boots);
        Assert.DoesNotContain(1058, c.Legendary); // composant (into)
        Assert.DoesNotContain(4403, c.Legendary); // hors Faille
    }

    [Fact]
    public void Core_build_ignore_bottes_composants_et_trinket()
    {
        var c = Load();
        Assert.Equal("3157-4645-3089", c.CoreBuildKey([3157, 3020, 4645, 1058, 3089, 0, 3340]));
        Assert.Null(c.CoreBuildKey([3157, 3020, 1058, 0, 0, 0, 3340]));
    }
}
