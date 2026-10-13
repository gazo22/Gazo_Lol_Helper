using RiftLens.UI.Components;

namespace RiftLens.UI.Services;

/// <summary>Pages de l'app + détection d'un Riot ID tapé (<c>Nom#TAG</c>).</summary>
public sealed class NavigationPaletteSource : IPaletteSource
{
    private static readonly PaletteEntry[] Pages =
    [
        new(PaletteGroup.Navigation, "Accueil", "", "home"),
        new(PaletteGroup.Navigation, "Tier list", "tierlist", "tier"),
        new(PaletteGroup.Navigation, "Champions", "champions", "champion"),
        new(PaletteGroup.Navigation, "Assistant de draft", "draft", "draft"),
        new(PaletteGroup.Navigation, "Paramètres", "settings", "settings"),
        new(PaletteGroup.Navigation, "À propos & mentions légales", "about", "info"),
    ];

    public ValueTask<IReadOnlyList<PaletteEntry>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var q = query.Trim();
        var results = new List<PaletteEntry>();

        var hash = q.LastIndexOf('#');
        if (hash > 0 && hash < q.Length - 1)
        {
            var name = q[..hash].Trim();
            var tag = q[(hash + 1)..].Trim();
            results.Add(new(PaletteGroup.Player, $"{name}#{tag}", Routes.Profile(name, tag), "user", "Ouvrir le profil"));
        }

        results.AddRange(q.Length == 0 ? Pages : Pages.Where(p => p.Label.Contains(q, StringComparison.CurrentCultureIgnoreCase)));
        return ValueTask.FromResult<IReadOnlyList<PaletteEntry>>(results);
    }
}
