namespace RiftLens.Domain.ValueObjects;

public enum Role
{
    Unknown = 0,
    Top,
    Jungle,
    Middle,
    Bottom,
    Support,
}

public static class RoleExtensions
{
    /// <summary>Convertit la valeur <c>teamPosition</c> de Match-V5.</summary>
    public static Role FromTeamPosition(string? position) => position?.ToUpperInvariant() switch
    {
        "TOP" => Role.Top,
        "JUNGLE" => Role.Jungle,
        "MIDDLE" => Role.Middle,
        "BOTTOM" => Role.Bottom,
        "UTILITY" => Role.Support,
        _ => Role.Unknown,
    };
}
