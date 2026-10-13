using System.Globalization;
using System.Text;
using RiftLens.UI.Components;

namespace RiftLens.UI.Services;

/// <summary>Recherche de champions dans la palette (insensible à la casse et aux accents).</summary>
public sealed class ChampionPaletteSource(GameAssets assets) : IPaletteSource
{
    private const int MaxResults = 6;

    public async ValueTask<IReadOnlyList<PaletteEntry>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var q = Normalize(query.Trim());
        if (q.Length < 2 || q.Contains('#', StringComparison.Ordinal))
        {
            return [];
        }

        await assets.EnsureLoadedAsync(cancellationToken);
        return assets.Champions
            .Select(c => (c, Name: Normalize(c.Name)))
            .Where(x => x.Name.Contains(q, StringComparison.Ordinal))
            .OrderBy(x => x.Name.StartsWith(q, StringComparison.Ordinal) ? 0 : 1)
            .Take(MaxResults)
            .Select(x => new PaletteEntry(PaletteGroup.Champion, x.c.Name, Routes.Champion(x.c.Key), "champion", "Builds & stats"))
            .ToList();
    }

    public static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
