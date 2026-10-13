using RiftLens.Contracts;

namespace RiftLens.Infrastructure.StaticData;

/// <summary>Données statiques du jeu (Data Dragon), version courante récupérée dynamiquement.</summary>
public interface IStaticDataService
{
    Task<StaticManifestDto> GetManifestAsync(CancellationToken cancellationToken = default);

    Task<ItemCategories> GetItemCategoriesAsync(CancellationToken cancellationToken = default);
}
