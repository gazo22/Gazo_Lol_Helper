using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.RateLimit;

namespace RiftLens.Infrastructure.Collection;

/// <summary>
/// Boucle du Collector : ladders → ids de matchs → téléchargement → agrégation.
/// Chaque cycle s'exécute dans un scope DI neuf ; l'état est entièrement persistant.
/// </summary>
public sealed partial class CollectorWorker(
    IServiceScopeFactory scopes,
    IOptions<CollectorOptions> options,
    IRiotKeyStore keys,
    TimeProvider time,
    ILogger<CollectorWorker> logger) : BackgroundService
{
    private DateTimeOffset _lastAggregation = DateTimeOffset.MinValue;
    private int _storedSinceAggregation;
    private CancellationTokenSource _wake = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, options.Value.Platform, options.Value.Tiers);
        keys.KeyChanged += OnKeyChanged;
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;
            try
            {
                delay = await RunCycleAsync(stoppingToken) ? TimeSpan.Zero : options.Value.IdleDelay;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RiotApiAuthException ex) when (ex.IsMissingKey)
            {
                // Clé pas encore saisie dans l'assistant : on dort jusqu'à ce qu'elle le soit.
                LogWaitingForKey(logger);
                delay = Timeout.InfiniteTimeSpan;
            }
            catch (RiotApiAuthException ex)
            {
                LogAuthFailure(logger, ex, options.Value.AuthFailureDelay.TotalMinutes);
                delay = options.Value.AuthFailureDelay;
            }
#pragma warning disable CA1031 // le worker doit survivre à toute erreur transitoire (SQL, réseau…)
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogCycleFailed(logger, ex);
                delay = options.Value.IdleDelay;
            }

            if (delay != TimeSpan.Zero)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _wake.Token);
                try { await Task.Delay(delay, time, linked.Token); }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Réveillé par une nouvelle clé Riot.
                    var old = Interlocked.Exchange(ref _wake, new CancellationTokenSource());
                    old.Dispose();
                }
                catch (OperationCanceledException) { break; }
            }
        }

        keys.KeyChanged -= OnKeyChanged;
        LogStopped(logger);
    }

    private void OnKeyChanged()
    {
        try { _wake.Cancel(); }
        catch (ObjectDisposedException) { /* déjà remplacé */ }
    }

    public override void Dispose()
    {
        _wake.Dispose();
        base.Dispose();
    }

    /// <summary>Renvoie true si du travail a été fait (on enchaîne alors sans attendre).</summary>
    private async Task<bool> RunCycleAsync(CancellationToken ct)
    {
        using var background = RiotRequestContext.WithPriority(RequestPriority.Background);
        await using var scope = scopes.CreateAsyncScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<CollectorPipeline>();
        var worked = false;

        if (await pipeline.IsSeedDueAsync(ct))
        {
            worked |= await pipeline.SeedAsync(ct) > 0;
        }

        var pending = await pipeline.CountPendingAsync(ct);
        if (pending < options.Value.MaxPending)
        {
            pending += await pipeline.CrawlAsync(ct);
        }

        var stored = pending > 0 ? await pipeline.FetchAsync(ct) : 0;
        _storedSinceAggregation += stored;
        worked |= stored > 0 || pending > 0;

        if (_storedSinceAggregation > 0 && time.GetUtcNow() - _lastAggregation >= options.Value.AggregateInterval)
        {
            await pipeline.AggregateAsync(ct);
            _lastAggregation = time.GetUtcNow();
            _storedSinceAggregation = 0;
        }

        return worked;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Collector démarré sur {Platform} ({Tiers})")]
    private static partial void LogStarted(ILogger logger, Domain.ValueObjects.Platform platform, IReadOnlyList<Domain.ValueObjects.RankTier> tiers);

    [LoggerMessage(Level = LogLevel.Information, Message = "Collector en attente d'une clé API Riot (assistant du Desktop)")]
    private static partial void LogWaitingForKey(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Collector arrêté proprement")]
    private static partial void LogStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Clé Riot refusée : pause de {Minutes} min (clé de développement expirée ?)")]
    private static partial void LogAuthFailure(ILogger logger, Exception ex, double minutes);

    [LoggerMessage(Level = LogLevel.Error, Message = "Cycle du Collector en échec, nouvel essai plus tard")]
    private static partial void LogCycleFailed(ILogger logger, Exception ex);
}
