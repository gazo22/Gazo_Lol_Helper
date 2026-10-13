using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiftLens.Infrastructure.Local;

namespace RiftLens.Infrastructure.Riot;

public enum RiotKeyState
{
    /// <summary>Aucune clé configurée.</summary>
    Missing,
    /// <summary>Clé présente, pas encore utilisée.</summary>
    Unknown,
    /// <summary>Dernier appel Riot accepté.</summary>
    Valid,
    /// <summary>Dernier appel refusé (401/403) : clé invalide ou expirée.</summary>
    Rejected,
}

/// <summary>Clé API Riot du serveur, modifiable à chaud (assistant du Desktop) sans redémarrage.</summary>
public interface IRiotKeyStore
{
    string? GetKey();

    RiotKeyState State { get; }

    event Action? KeyChanged;

    Task SetKeyAsync(string key, CancellationToken ct = default);

    void ReportAccepted();

    void ReportRejected();
}

/// <summary>
/// Priorité : configuration (<c>Riot:ApiKey</c> via user-secrets / variable d'environnement), sinon fichier
/// <c>riot.key</c> du dossier de données. Sous Windows, le fichier est chiffré par DPAPI (portée machine) et son ACL
/// est restreinte à SYSTEM et aux Administrateurs.
/// </summary>
public sealed partial class RiotKeyStore(
    IOptions<RiotApiOptions> riotOptions,
    IOptions<LocalModeOptions> localOptions,
    ILogger<RiotKeyStore> logger) : IRiotKeyStore
{
    private static readonly byte[] Entropy = "RiftLens.RiotKey.v1"u8.ToArray();
    private readonly Lock _gate = new();
    private string? _cached;
    private bool _loaded;
    private RiotKeyState? _state;

    public event Action? KeyChanged;

    public RiotKeyState State
    {
        get
        {
            var key = GetKey();
            return key is null ? RiotKeyState.Missing : _state ?? RiotKeyState.Unknown;
        }
    }

    private string KeyPath => Path.Combine(localOptions.Value.DataDirectory, LocalModeOptions.RiotKeyFileName);

    public string? GetKey()
    {
        if (!string.IsNullOrWhiteSpace(riotOptions.Value.ApiKey))
        {
            return riotOptions.Value.ApiKey;
        }

        lock (_gate)
        {
            if (!_loaded)
            {
                _cached = Load();
                _loaded = true;
            }

            return _cached;
        }
    }

    public async Task SetKeyAsync(string key, CancellationToken ct = default)
    {
        var data = Encoding.UTF8.GetBytes(key.Trim());
        if (OperatingSystem.IsWindows())
        {
            data = ProtectedData.Protect(data, Entropy, DataProtectionScope.LocalMachine);
        }

        Directory.CreateDirectory(localOptions.Value.DataDirectory);
        await File.WriteAllBytesAsync(KeyPath, data, ct);
        Restrict(KeyPath);

        lock (_gate)
        {
            _cached = key.Trim();
            _loaded = true;
            _state = RiotKeyState.Valid; // validée par l'appelant avant enregistrement
        }

        LogKeyUpdated(logger);
        KeyChanged?.Invoke();
    }

    public void ReportAccepted() => _state = RiotKeyState.Valid;

    public void ReportRejected()
    {
        if (_state != RiotKeyState.Rejected)
        {
            _state = RiotKeyState.Rejected;
            LogKeyRejected(logger);
        }
    }

    private string? Load()
    {
        try
        {
            if (!File.Exists(KeyPath))
            {
                return null;
            }

            var data = File.ReadAllBytes(KeyPath);
            if (OperatingSystem.IsWindows())
            {
                data = ProtectedData.Unprotect(data, Entropy, DataProtectionScope.LocalMachine);
            }

            var key = Encoding.UTF8.GetString(data).Trim();
            return key.Length == 0 ? null : key;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            LogLoadFailed(logger, ex);
            return null;
        }
    }

    private static void Restrict(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new FileSecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
            new FileInfo(path).SetAccessControl(security);
        }
        else
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Clé API Riot mise à jour")]
    private static partial void LogKeyUpdated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Clé API Riot refusée par Riot (invalide ou expirée)")]
    private static partial void LogKeyRejected(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Lecture de la clé Riot impossible")]
    private static partial void LogLoadFailed(ILogger logger, Exception ex);
}
