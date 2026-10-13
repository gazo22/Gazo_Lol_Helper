using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace RiftLens.LocalClient.Connection;

public interface ILockfileLocator
{
    /// <summary>Identifiants du client LoL s'il est lancé, sinon null.</summary>
    LcuCredentials? Locate();
}

/// <summary>
/// Cherche le lockfile à côté de l'exécutable <c>LeagueClientUx</c> en cours d'exécution,
/// puis dans les dossiers d'installation configurés.
/// </summary>
public sealed class LockfileLocator(IOptions<LocalClientOptions> options) : ILockfileLocator
{
    private const string LockfileName = "lockfile";

    public LcuCredentials? Locate()
    {
        foreach (var directory in CandidateDirectories())
        {
            var path = Path.Combine(directory, LockfileName);
            if (TryRead(path) is { } credentials)
            {
                return credentials;
            }
        }

        return null;
    }

    private IEnumerable<string> CandidateDirectories()
    {
        foreach (var name in new[] { "LeagueClientUx", "LeagueClient" })
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(name); }
            catch (InvalidOperationException) { continue; }

            foreach (var process in processes)
            {
                string? directory = null;
                try { directory = Path.GetDirectoryName(process.MainModule?.FileName); }
                catch (System.ComponentModel.Win32Exception) { /* processus protégé : on passe aux chemins configurés */ }
                catch (InvalidOperationException) { /* processus terminé entre-temps */ }
                finally { process.Dispose(); }

                if (directory is not null)
                {
                    yield return directory;
                }
            }
        }

        foreach (var path in options.Value.InstallPaths)
        {
            yield return path;
        }
    }

    private static LcuCredentials? TryRead(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            // Le client garde le fichier ouvert : lecture en partage.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return LcuCredentials.TryParseLockfile(reader.ReadToEnd(), out var credentials) ? credentials : null;
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
