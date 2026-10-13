namespace RiftLens.Infrastructure.Riot.RateLimit;

/// <summary>
/// Ensemble de fenêtres glissantes partageant un même historique de requêtes
/// (limite applicative d'une route, ou limite d'une méthode sur une route).
/// La fenêtre glissante est plus stricte que la fenêtre fixe de Riot : on ne peut donc pas la dépasser.
/// </summary>
internal sealed class RateLimitBucket(IReadOnlyList<RateLimitWindow> windows)
{
    private readonly Queue<DateTimeOffset> _history = new();
    private IReadOnlyList<RateLimitWindow> _windows = windows;

    public DateTimeOffset FrozenUntil { get; set; } = DateTimeOffset.MinValue;

    public IReadOnlyList<RateLimitWindow> Windows => _windows;

    public void UpdateWindows(IReadOnlyList<RateLimitWindow> windows)
    {
        if (windows.Count > 0)
        {
            _windows = windows;
        }
    }

    /// <summary>Délai avant qu'une requête soit autorisée ; zéro si possible immédiatement.</summary>
    public TimeSpan GetDelay(DateTimeOffset now, double share)
    {
        var delay = FrozenUntil > now ? FrozenUntil - now : TimeSpan.Zero;
        if (_windows.Count == 0)
        {
            return delay;
        }

        Trim(now);
        var history = _history.ToArray(); // du plus ancien au plus récent
        foreach (var window in _windows)
        {
            var allowed = Math.Max(1, (int)Math.Floor(window.Limit * share));
            var start = now - window.Window;
            var inWindow = 0;
            for (var i = history.Length - 1; i >= 0 && history[i] > start; i--)
            {
                inWindow++;
            }

            if (inWindow >= allowed)
            {
                // Une place se libère quand la allowed-ième requête la plus récente sort de la fenêtre.
                var releasing = history[history.Length - allowed];
                var wait = releasing + window.Window - now;
                if (wait > delay)
                {
                    delay = wait;
                }
            }
        }

        return delay;
    }

    public void Record(DateTimeOffset now) => _history.Enqueue(now);

    private void Trim(DateTimeOffset now)
    {
        var longest = _windows.Max(w => w.Window);
        while (_history.Count > 0 && _history.Peek() <= now - longest)
        {
            _history.Dequeue();
        }
    }
}
