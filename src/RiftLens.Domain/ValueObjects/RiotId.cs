using System.Diagnostics.CodeAnalysis;

namespace RiftLens.Domain.ValueObjects;

/// <summary>Identifiant Riot au format <c>GameName#TAG</c>.</summary>
public readonly record struct RiotId(string GameName, string TagLine)
{
    public const int MaxGameNameLength = 16;
    public const int MinTagLength = 3;
    public const int MaxTagLength = 5;

    public override string ToString() => $"{GameName}#{TagLine}";

    public static bool TryParse(string? value, [NotNullWhen(true)] out RiotId? riotId)
    {
        riotId = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var hash = value.LastIndexOf('#');
        if (hash <= 0 || hash == value.Length - 1)
        {
            return false;
        }

        var name = value[..hash].Trim();
        var tag = value[(hash + 1)..].Trim();
        if (name.Length is 0 or > MaxGameNameLength || tag.Length is < MinTagLength or > MaxTagLength)
        {
            return false;
        }

        riotId = new RiotId(name, tag);
        return true;
    }
}
