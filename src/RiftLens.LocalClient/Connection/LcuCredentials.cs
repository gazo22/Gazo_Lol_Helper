using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace RiftLens.LocalClient.Connection;

/// <summary>Port et mot de passe de l'API locale du client (LCU), lus dans le <c>lockfile</c>.</summary>
public sealed partial record LcuCredentials(int Port, string Password, string Protocol = "https")
{
    public const string Username = "riot";

    public Uri BaseUri => new($"{Protocol}://127.0.0.1:{Port.ToString(CultureInfo.InvariantCulture)}/");

    public Uri WebSocketUri => new($"wss://127.0.0.1:{Port.ToString(CultureInfo.InvariantCulture)}/");

    public AuthenticationHeaderValue AuthorizationHeader =>
        new("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Password}")));

    /// <summary>Format : <c>LeagueClient:&lt;pid&gt;:&lt;port&gt;:&lt;password&gt;:&lt;protocol&gt;</c>.</summary>
    public static bool TryParseLockfile(string? content, [NotNullWhen(true)] out LcuCredentials? credentials)
    {
        credentials = null;
        var parts = content?.Trim().Split(':');
        if (parts is not { Length: 5 }
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            || port is <= 0 or > 65535
            || parts[3].Length == 0)
        {
            return false;
        }

        credentials = new LcuCredentials(port, parts[3], parts[4]);
        return true;
    }

    /// <summary>Repli : arguments de <c>LeagueClientUx</c> (<c>--app-port=…</c> et <c>--remoting-auth-token=…</c>).</summary>
    public static bool TryParseCommandLine(string? commandLine, [NotNullWhen(true)] out LcuCredentials? credentials)
    {
        credentials = null;
        if (string.IsNullOrEmpty(commandLine))
        {
            return false;
        }

        var port = PortRegex().Match(commandLine);
        var token = TokenRegex().Match(commandLine);
        if (!port.Success || !token.Success)
        {
            return false;
        }

        credentials = new LcuCredentials(int.Parse(port.Groups[1].Value, CultureInfo.InvariantCulture), token.Groups[1].Value);
        return true;
    }

    [GeneratedRegex("--app-port=\"?(\\d{1,5})")]
    private static partial Regex PortRegex();

    [GeneratedRegex("--remoting-auth-token=\"?([\\w-]+)")]
    private static partial Regex TokenRegex();

    // Ne jamais journaliser le mot de passe.
    public override string ToString() => $"LCU 127.0.0.1:{Port}";
}
