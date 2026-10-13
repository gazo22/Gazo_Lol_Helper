using System.Globalization;

namespace RiftLens.Infrastructure.Riot.RateLimit;

/// <summary>Une limite Riot « N requêtes par T secondes ».</summary>
public readonly record struct RateLimitWindow(int Limit, TimeSpan Window)
{
    /// <summary>Analyse le format des en-têtes Riot : <c>20:1,100:120</c>.</summary>
    public static IReadOnlyList<RateLimitWindow> ParseHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return [];
        }

        var result = new List<RateLimitWindow>();
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split(':');
            if (pair.Length == 2
                && int.TryParse(pair[0], NumberStyles.None, CultureInfo.InvariantCulture, out var limit)
                && int.TryParse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
                && limit > 0 && seconds > 0)
            {
                result.Add(new RateLimitWindow(limit, TimeSpan.FromSeconds(seconds)));
            }
        }

        return result;
    }
}
