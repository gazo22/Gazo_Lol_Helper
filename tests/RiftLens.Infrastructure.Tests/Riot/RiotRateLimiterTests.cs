using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RiftLens.Infrastructure.Riot.RateLimit;

namespace RiftLens.Infrastructure.Tests.Riot;

public class RiotRateLimiterTests
{
    private const string Route = "euw1";
    private const string Method = "summoner-v4.by-puuid";
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-13T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));

    private RiotRateLimiter Create(double share = 1.0, string limits = "20:1,100:120") =>
        new(Options.Create(new RiotRateLimitOptions { BudgetShare = share, DefaultAppLimits = limits }), _time, NullLogger<RiotRateLimiter>.Instance);

    private static Task Acquire(RiotRateLimiter limiter, RequestPriority priority = RequestPriority.Interactive, string route = Route) =>
        limiter.AcquireAsync(route, Method, priority, CancellationToken.None);

    [Fact]
    public async Task Vingt_requetes_par_seconde_puis_attente()
    {
        var limiter = Create();
        for (var i = 0; i < 20; i++)
        {
            await Acquire(limiter);
        }

        var next = Acquire(limiter);
        Assert.False(next.IsCompleted);

        _time.Advance(TimeSpan.FromMilliseconds(999));
        Assert.False(next.IsCompleted);

        _time.Advance(TimeSpan.FromMilliseconds(2));
        await next.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Cent_requetes_par_deux_minutes()
    {
        var limiter = Create();
        for (var i = 0; i < 100; i++)
        {
            if (i > 0 && i % 20 == 0)
            {
                _time.Advance(TimeSpan.FromSeconds(1.01));
            }

            await Acquire(limiter);
        }

        var next = Acquire(limiter);
        _time.Advance(TimeSpan.FromSeconds(60));
        Assert.False(next.IsCompleted);

        // La première requête date de ~4 s avant la 100e : elle sort de la fenêtre de 120 s.
        _time.Advance(TimeSpan.FromSeconds(60));
        await next.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Les_routes_sont_independantes()
    {
        var limiter = Create(limits: "1:10");
        await Acquire(limiter, route: "euw1");
        await Acquire(limiter, route: "europe").WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task La_part_de_budget_reduit_la_limite()
    {
        var limiter = Create(share: 0.5);
        for (var i = 0; i < 10; i++)
        {
            await Acquire(limiter);
        }

        Assert.False(Acquire(limiter).IsCompleted);
    }

    [Fact]
    public async Task Les_limites_de_methode_recues_sont_appliquees()
    {
        var limiter = Create();
        limiter.UpdateLimits(Route, Method, "20:1,100:120", "2:10");
        await Acquire(limiter);
        await Acquire(limiter);

        var third = Acquire(limiter);
        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.False(third.IsCompleted);
        _time.Advance(TimeSpan.FromSeconds(5.1));
        await third.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Un_429_gele_la_route_jusqu_au_retry_after()
    {
        var limiter = Create();
        limiter.Freeze(Route, Method, RateLimitScope.Application, TimeSpan.FromSeconds(7));

        var blocked = Acquire(limiter);
        _time.Advance(TimeSpan.FromSeconds(6.9));
        Assert.False(blocked.IsCompleted);
        _time.Advance(TimeSpan.FromSeconds(0.2));
        await blocked.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Les_requetes_interactives_passent_avant_le_fond()
    {
        var limiter = Create(limits: "1:1");
        await Acquire(limiter);

        var background = Acquire(limiter, RequestPriority.Background);
        var interactive = Acquire(limiter, RequestPriority.Interactive);

        _time.Advance(TimeSpan.FromSeconds(1.01));
        await interactive.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(background.IsCompleted);

        for (var i = 0; i < 30 && !background.IsCompleted; i++)
        {
            _time.Advance(TimeSpan.FromMilliseconds(100));
            await Task.Yield();
        }

        await background.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
