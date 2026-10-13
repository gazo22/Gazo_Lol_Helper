namespace RiftLens.UI.Services;

public enum PaletteGroup
{
    Player,
    Champion,
    Navigation,
}

public sealed record PaletteEntry(PaletteGroup Group, string Label, string Href, string Icon, string? Hint = null);

/// <summary>Source d'entrées pour la palette (navigation, champions, joueurs récents…).</summary>
public interface IPaletteSource
{
    ValueTask<IReadOnlyList<PaletteEntry>> SearchAsync(string query, CancellationToken cancellationToken);
}
