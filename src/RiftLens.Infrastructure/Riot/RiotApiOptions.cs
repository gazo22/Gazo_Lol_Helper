using System.ComponentModel.DataAnnotations;
using RiftLens.Domain.ValueObjects;

namespace RiftLens.Infrastructure.Riot;

public sealed class RiotApiOptions
{
    public const string SectionName = "Riot";

    /// <summary>Clé API Riot : user-secrets ou variable d'environnement <c>Riot__ApiKey</c>. Jamais commitée.</summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public Platform DefaultPlatform { get; set; } = Platform.Euw1;
}
