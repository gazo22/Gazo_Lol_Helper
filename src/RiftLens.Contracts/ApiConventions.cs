namespace RiftLens.Contracts;

public static class ApiConventions
{
    /// <summary>En-tête portant la clé d'accès partagée entre le Desktop et l'Api.</summary>
    public const string AccessKeyHeader = "X-RiftLens-Key";
}

public sealed record ServerInfoDto(string Version, string DefaultPlatform, string? DataDragonVersion);
