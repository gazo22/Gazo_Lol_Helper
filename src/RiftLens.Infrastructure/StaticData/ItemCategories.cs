namespace RiftLens.Infrastructure.StaticData;

/// <summary>Classement des objets utile à l'extraction des builds.</summary>
public sealed record ItemCategories(IReadOnlySet<int> Legendary, IReadOnlySet<int> Boots, IReadOnlySet<int> Trinkets)
{
    /// <summary>Bottes de base (non terminées).</summary>
    public const int BasicBoots = 1001;

    public static ItemCategories Empty { get; } = new(new HashSet<int>(), new HashSet<int>(), new HashSet<int>());

    /// <summary>Objet « terminé » pour l'ordre d'achat : légendaire ou bottes de niveau 2.</summary>
    public bool IsCompleted(int itemId) => Legendary.Contains(itemId) || (Boots.Contains(itemId) && itemId != BasicBoots);

    /// <summary>Les 3 premiers objets légendaires (ordre des slots, hors bottes), ou null s'il y en a moins de 3.</summary>
    public string? CoreBuildKey(IReadOnlyList<int> items)
    {
        var core = items.Take(6).Where(i => Legendary.Contains(i) && !Boots.Contains(i)).Distinct().Take(3).ToArray();
        return core.Length == 3 ? string.Join('-', core) : null;
    }
}
