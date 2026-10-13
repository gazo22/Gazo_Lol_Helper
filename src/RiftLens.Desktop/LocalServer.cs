using System.IO;

namespace RiftLens.Desktop;

/// <summary>Accès aux fichiers partagés par le service « RiftLens Server » installé sur ce poste.</summary>
internal static class LocalServer
{
    private static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RiftLens");

    /// <summary>Clé d'accès écrite par le service ; null si le service n'a pas encore démarré.</summary>
    public static string? TryReadAccessKey()
    {
        try
        {
            var path = Path.Combine(DataDirectory, "access.key");
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
