namespace RiftLens.Contracts;

public static class ApiConventions
{
    /// <summary>En-tête portant la clé d'accès partagée entre le Desktop et l'Api.</summary>
    public const string AccessKeyHeader = "X-RiftLens-Key";
}

/// <param name="RiotKey">Missing, Unknown, Valid ou Rejected.</param>
/// <param name="LocalMode">Serveur installé sur ce poste (l'assistant peut alors y enregistrer la clé Riot).</param>
public sealed record ServerInfoDto(string Version, string DefaultPlatform, string? DataDragonVersion, string RiotKey = "Unknown", bool LocalMode = false);

public sealed record RiotKeyRequest(string Key);

public static class RiotKeyStates
{
    public const string Missing = "Missing";
    public const string Unknown = "Unknown";
    public const string Valid = "Valid";
    public const string Rejected = "Rejected";
}
