namespace RiftLens.Api.Security;

public sealed class AccessKeyOptions
{
    public const string SectionName = "Access";

    /// <summary>Clés acceptées (rotation possible). À définir via user-secrets / <c>Access__Keys__0</c>.</summary>
    public List<string> Keys { get; } = [];
}
