using RiftLens.Contracts;
using RiftLens.Infrastructure.StaticData;

namespace RiftLens.Infrastructure.Tests.Support;

internal sealed class FixtureStaticData : IStaticDataService
{
    public Task<StaticManifestDto> GetManifestAsync(CancellationToken cancellationToken = default) => Task.FromResult(StaticManifestDto.Empty);

    public Task<ItemCategories> GetItemCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using var doc = Fixture.Doc("ddragon/item.json");
        return Task.FromResult(DataDragonService.ParseItemCategories(doc));
    }
}
