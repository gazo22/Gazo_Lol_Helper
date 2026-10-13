using RiftLens.Contracts;

namespace RiftLens.UI.Services;

/// <summary>Ajoute la clé d'accès du backend ; en mode local, elle est relue jusqu'à ce que le service l'ait créée.</summary>
public sealed class AccessKeyHandler(BackendOptions options) : DelegatingHandler
{
    private string? _cached;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = options.AccessKey.Length > 0 ? options.AccessKey : _cached ??= options.AccessKeyProvider?.Invoke();
        if (!string.IsNullOrEmpty(key))
        {
            request.Headers.Remove(ApiConventions.AccessKeyHeader);
            request.Headers.Add(ApiConventions.AccessKeyHeader, key);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
