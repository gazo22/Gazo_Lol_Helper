using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace RiftLens.Domain.ValueObjects;

/// <summary>Patch majeur.mineur (ex. 15.20) extrait de <c>gameVersion</c>.</summary>
public readonly record struct Patch(int Major, int Minor) : IComparable<Patch>
{
    public override string ToString() => $"{Major}.{Minor}";

    public int CompareTo(Patch other) => Major != other.Major ? Major.CompareTo(other.Major) : Minor.CompareTo(other.Minor);

    public static bool operator <(Patch left, Patch right) => left.CompareTo(right) < 0;
    public static bool operator >(Patch left, Patch right) => left.CompareTo(right) > 0;
    public static bool operator <=(Patch left, Patch right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Patch left, Patch right) => left.CompareTo(right) >= 0;

    /// <summary>Accepte "15.20", "15.20.1" ou "15.20.715.1234".</summary>
    public static bool TryParse(string? version, [NotNullWhen(true)] out Patch? patch)
    {
        patch = null;
        var parts = version?.Split('.');
        if (parts is not { Length: >= 2 }
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minor))
        {
            return false;
        }

        patch = new Patch(major, minor);
        return true;
    }
}
