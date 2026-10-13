namespace RiftLens.Collector;

/// <summary>
/// Collecte des matchs high-elo et agrégation (implémentée en phase 3).
/// </summary>
public sealed partial class CollectorWorker(ILogger<CollectorWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Collector démarré (pipeline non encore implémenté)")]
    private static partial void LogStarted(ILogger logger);
}
