using System.Security.Cryptography;
using RiftLens.Infrastructure.Local;

namespace RiftLens.Api.Security;

/// <summary>
/// En mode local, la clé d'accès Desktop ↔ serveur est générée au premier démarrage et écrite dans
/// <c>%ProgramData%\RiftLens\access.key</c>, où le Desktop la lit. Aucune configuration manuelle.
/// </summary>
public static class LocalAccessKey
{
    public static string EnsureCreated(string dataDirectory)
    {
        var path = Path.Combine(dataDirectory, LocalModeOptions.AccessKeyFileName);
        if (File.Exists(path) && File.ReadAllText(path).Trim() is { Length: >= 32 } existing)
        {
            return existing;
        }

        Directory.CreateDirectory(dataDirectory);
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        File.WriteAllText(path, key);
        return key;
    }
}
