using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RiftLens.Infrastructure.Riot.RateLimit;

/// <summary>
/// File d'attente centralisée par route (euw1, europe…) : respecte la limite applicative de la route
/// et la limite propre à chaque méthode, mises à jour depuis les en-têtes de réponse Riot.
/// </summary>
public sealed partial class RiotRateLimiter
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, RateLimitBucket> _appBuckets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RateLimitBucket> _methodBuckets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _interactiveWaiting = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyList<RateLimitWindow> _defaultAppLimits;
    private readonly RiotRateLimitOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<RiotRateLimiter> _logger;

    public RiotRateLimiter(IOptions<RiotRateLimitOptions> options, TimeProvider time, ILogger<RiotRateLimiter> logger)
    {
        _options = options.Value;
        if (_options.BudgetShare is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "BudgetShare doit être dans ]0, 1].");
        }

        _defaultAppLimits = RateLimitWindow.ParseHeader(_options.DefaultAppLimits);
        _time = time;
        _logger = logger;
    }

    /// <summary>Attend qu'une requête puisse partir sur <paramref name="route"/> pour <paramref name="method"/>.</summary>
    public async Task AcquireAsync(string route, string method, RequestPriority priority, CancellationToken cancellationToken)
    {
        var interactive = priority == RequestPriority.Interactive;
        if (interactive)
        {
            lock (_gate) { _interactiveWaiting[route] = _interactiveWaiting.GetValueOrDefault(route) + 1; }
        }

        try
        {
            while (true)
            {
                TimeSpan delay;
                lock (_gate)
                {
                    var now = _time.GetUtcNow();
                    var app = GetAppBucket(route);
                    var methodBucket = GetMethodBucket(route, method);
                    delay = Max(app.GetDelay(now, _options.BudgetShare), methodBucket.GetDelay(now, _options.BudgetShare));

                    // Une requête de fond cède la place tant que des requêtes interactives attendent sur la route.
                    if (delay <= TimeSpan.Zero && !interactive && _interactiveWaiting.GetValueOrDefault(route) > 0)
                    {
                        delay = TimeSpan.FromMilliseconds(50);
                    }

                    if (delay <= TimeSpan.Zero)
                    {
                        app.Record(now);
                        methodBucket.Record(now);
                        return;
                    }
                }

                LogWaiting(_logger, route, method, delay.TotalMilliseconds);
                await Task.Delay(delay, _time, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (interactive)
            {
                lock (_gate) { _interactiveWaiting[route]--; }
            }
        }
    }

    /// <summary>Met à jour les limites depuis <c>X-App-Rate-Limit</c> et <c>X-Method-Rate-Limit</c>.</summary>
    public void UpdateLimits(string route, string method, string? appLimits, string? methodLimits)
    {
        lock (_gate)
        {
            GetAppBucket(route).UpdateWindows(RateLimitWindow.ParseHeader(appLimits));
            GetMethodBucket(route, method).UpdateWindows(RateLimitWindow.ParseHeader(methodLimits));
        }
    }

    /// <summary>Gèle la route (limite applicative) ou la méthode jusqu'à la fin du <c>Retry-After</c>.</summary>
    public void Freeze(string route, string method, RateLimitScope scope, TimeSpan retryAfter)
    {
        lock (_gate)
        {
            var until = _time.GetUtcNow() + retryAfter;
            var bucket = scope == RateLimitScope.Application ? GetAppBucket(route) : GetMethodBucket(route, method);
            if (until > bucket.FrozenUntil)
            {
                bucket.FrozenUntil = until;
            }
        }

        LogFrozen(_logger, route, method, scope, retryAfter.TotalSeconds);
    }

    private RateLimitBucket GetAppBucket(string route)
    {
        if (!_appBuckets.TryGetValue(route, out var bucket))
        {
            bucket = new RateLimitBucket(_defaultAppLimits);
            _appBuckets[route] = bucket;
        }

        return bucket;
    }

    private RateLimitBucket GetMethodBucket(string route, string method)
    {
        var key = route + "|" + method;
        if (!_methodBuckets.TryGetValue(key, out var bucket))
        {
            // Limites de méthode inconnues tant que Riot ne les a pas renvoyées.
            bucket = new RateLimitBucket([]);
            _methodBuckets[key] = bucket;
        }

        return bucket;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Rate limit : attente de {DelayMs} ms sur {Route} / {Method}")]
    private static partial void LogWaiting(ILogger logger, string route, string method, double delayMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "429 reçu sur {Route} / {Method} ({Scope}) : pause de {Seconds} s")]
    private static partial void LogFrozen(ILogger logger, string route, string method, RateLimitScope scope, double seconds);
}

public enum RateLimitScope
{
    Application,
    Method,
    Service,
}
