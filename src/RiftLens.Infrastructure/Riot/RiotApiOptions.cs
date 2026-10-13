using RiftLens.Domain.ValueObjects;

namespace RiftLens.Infrastructure.Riot;

public sealed class RiotApiOptions
{
    public const string SectionName = "Riot";

    /// <summary>
    /// Clé API Riot fixe (user-secrets / <c>Riot__ApiKey</c>), prioritaire. Vide en mode local : la clé est alors saisie
    /// dans l'assistant du Desktop et gérée par <see cref="IRiotKeyStore"/>. Jamais commitée.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    public Platform DefaultPlatform { get; set; } = Platform.Euw1;
}
