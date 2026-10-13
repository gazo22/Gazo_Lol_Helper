namespace RiftLens.Infrastructure.StaticData;

/// <summary>Classement des objets utile à l'extraction des builds.</summary>
public sealed record ItemCategories(IReadOnlySet<int> Legendary, IReadOnlySet<int> Boots)
{
    public static ItemCategories Empty { get; } = new(new HashSet<int>(), new HashSet<int>());

    /// <summary>Les 3 premiers objets légendaires (ordre des slots, hors bottes), ou null s'il y en a moins de 3.</summary>
    public string? CoreBuildKey(IReadOnlyList<int> items)
    {
        var core = items.Take(6).Where(i => Legendary.Contains(i) && !Boots.Contains(i)).Distinct().Take(3).ToArray();
        return core.Length == 3 ? string.Join('-', core) : null;
    }
}
