using System.Net;
using System.Net.Security;

namespace RiftLens.LocalClient.Connection;

/// <summary>
/// Le client LoL expose des certificats auto-signés. On ne les accepte <b>que</b> pour 127.0.0.1 :
/// toute autre destination garde la validation TLS standard.
/// </summary>
public static class LoopbackCertificatePolicy
{
    public static bool IsAcceptable(Uri? target, SslPolicyErrors errors) =>
        errors == SslPolicyErrors.None
        || (target is not null && IPAddress.TryParse(target.Host, out var ip) && IPAddress.IsLoopback(ip));

    /// <summary>Handler HTTP dont la validation de certificat dépend de l'URI de chaque requête.</summary>
    public static HttpClientHandler CreateHandler() => new()
    {
        ServerCertificateCustomValidationCallback = (request, _, _, errors) => IsAcceptable(request.RequestUri, errors),
    };

    public static RemoteCertificateValidationCallback ForUri(Uri target) =>
        (_, _, _, errors) => IsAcceptable(target, errors);
}
