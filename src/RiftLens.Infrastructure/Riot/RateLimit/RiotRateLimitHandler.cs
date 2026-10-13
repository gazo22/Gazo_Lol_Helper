using System.Net;
using Microsoft.Extensions.Options;

namespace RiftLens.Infrastructure.Riot.RateLimit;

/// <summary>
/// Branche le <see cref="RiotRateLimiter"/> sur chaque appel Riot, ajoute la clé API
/// et rejoue les 429 après le délai imposé par <c>Retry-After</c>.
/// </summary>
internal sealed class RiotRateLimitHandler(
    RiotRateLimiter limiter,
    IOptions<RiotRateLimitOptions> rateOptions,
    IRiotKeyStore keys,
    TimeProvider time) : DelegatingHandler
{
    public static readonly HttpRequestOptionsKey<string> MethodKey = new("riftlens.riot.method");
    public static readonly HttpRequestOptionsKey<RequestPriority> PriorityKey = new("riftlens.riot.priority");

    /// <summary>Clé à utiliser pour cette requête seulement (validation d'une nouvelle clé avant enregistrement).</summary>
    public static readonly HttpRequestOptionsKey<string> ExplicitKeyKey = new("riftlens.riot.explicit-key");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var route = request.RequestUri?.Host.Split('.')[0] ?? throw new InvalidOperationException("URI Riot manquante.");
        var method = request.Options.TryGetValue(MethodKey, out var m) ? m : request.RequestUri.AbsolutePath;
        var priority = request.Options.TryGetValue(PriorityKey, out var p) ? p : RiotRequestContext.Priority ?? rateOptions.Value.DefaultPriority;
        var isExplicit = request.Options.TryGetValue(ExplicitKeyKey, out var explicitKey);
        var key = isExplicit ? explicitKey : keys.GetKey();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new RiotApiAuthException("Aucune clé API Riot configurée.") { IsMissingKey = true };
        }

        request.Headers.Remove("X-Riot-Token");
        request.Headers.Add("X-Riot-Token", key);

        for (var attempt = 0; ; attempt++)
        {
            await limiter.AcquireAsync(route, method, priority, cancellationToken).ConfigureAwait(false);
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            limiter.UpdateLimits(route, method, Header(response, "X-App-Rate-Limit"), Header(response, "X-Method-Rate-Limit"));
            if (!isExplicit)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { keys.ReportRejected(); }
                else if (response.IsSuccessStatusCode) { keys.ReportAccepted(); }
            }

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= rateOptions.Value.MaxRetries)
            {
                return response;
            }

            var retryAfter = response.Headers.RetryAfter?.Delta
                ?? (response.Headers.RetryAfter?.Date is { } date ? date - time.GetUtcNow() : rateOptions.Value.DefaultRetryAfter);
            var scope = Header(response, "X-Rate-Limit-Type")?.ToUpperInvariant() switch
            {
                "APPLICATION" => RateLimitScope.Application,
                "METHOD" => RateLimitScope.Method,
                _ => RateLimitScope.Service,
            };
            response.Dispose();

            if (scope == RateLimitScope.Service)
            {
                // Surcharge côté Riot : pas de gel de la file, juste un délai pour cette requête.
                await Task.Delay(retryAfter, time, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                limiter.Freeze(route, method, scope, retryAfter);
            }
        }
    }

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(',', values) : null;
}
