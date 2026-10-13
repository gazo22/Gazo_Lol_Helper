using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiftLens.Contracts;
using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Persistence;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.RateLimit;
using RiftLens.Infrastructure.Stats;

namespace RiftLens.Infrastructure.Collection;

/// <summary>
/// Étapes du Collector. Tout l'état (joueurs, file de matchs) est en base : après une interruption,
/// le cycle suivant reprend exactement où il s'était arrêté.
/// </summary>
public sealed partial class CollectorPipeline(
    RiftLensDbContext db,
    IRiotApiClient riot,
    IStatsAggregator aggregator,
    IOptions<CollectorOptions> options,
    TimeProvider time,
    ILogger<CollectorPipeline> logger)
{
    private CollectorOptions Options => options.Value;

    public async Task<bool> IsSeedDueAsync(CancellationToken ct)
    {
        var last = await db.CollectorPlayers.MaxAsync(p => (DateTimeOffset?)p.SeenAt, ct);
        return last is null || time.GetUtcNow() - last > Options.SeedInterval;
    }

    /// <summary>Importe les ladders apex. Renvoie le nombre de joueurs vus.</summary>
    public async Task<int> SeedAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var seen = 0;
        foreach (var tier in Options.Tiers)
        {
            var league = await riot.GetApexLeagueAsync(Options.Platform, tier, ct);
            if (league is null)
            {
                continue;
            }

            var puuids = league.Entries.Select(e => e.Puuid).ToList();
            var existing = await db.CollectorPlayers.Where(p => puuids.Contains(p.Puuid)).ToDictionaryAsync(p => p.Puuid, ct);
            foreach (var entry in league.Entries)
            {
                if (!existing.TryGetValue(entry.Puuid, out var player))
                {
                    player = new CollectorPlayer { Puuid = entry.Puuid };
                    db.CollectorPlayers.Add(player);
                }

                player.Platform = Options.Platform;
                player.Tier = tier;
                player.LeaguePoints = entry.LeaguePoints;
                player.SeenAt = now;
            }

            await db.SaveChangesAsync(ct);
            seen += league.Entries.Count;
        }

        db.ChangeTracker.Clear();
        LogSeeded(logger, seen);
        return seen;
    }

    public Task<int> CountPendingAsync(CancellationToken ct) =>
        db.QueuedMatches.CountAsync(q => q.Status == QueueStatus.Pending, ct);

    /// <summary>Parcourt l'historique des joueurs les moins récemment visités. Renvoie le nombre de matchs mis en file.</summary>
    public async Task<int> CrawlAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var players = await db.CollectorPlayers
            .Where(p => p.Platform == Options.Platform)
            .OrderBy(p => p.LastCrawledAt.HasValue).ThenBy(p => p.LastCrawledAt)
            .Take(Options.PlayersPerCycle)
            .ToListAsync(ct);

        var enqueued = 0;
        foreach (var player in players)
        {
            var ids = await riot.GetMatchIdsAsync(Options.Platform.ToRegional(), player.Puuid,
                new MatchIdQuery(0, Options.MatchesPerPlayer, Queues.RankedSolo, RequestPriority.Background, now.AddDays(-Options.LookbackDays)), ct);

            var queued = await db.QueuedMatches.Where(q => ids.Contains(q.MatchId)).Select(q => q.MatchId).ToListAsync(ct);
            var stored = await db.Matches.Where(m => ids.Contains(m.MatchId)).ToListAsync(ct);
            foreach (var match in stored.Where(m => m.SourceTier is null))
            {
                // Déjà téléchargé via un profil : on le rattache au ladder sans le retélécharger.
                match.SourceTier = player.Tier;
            }

            var known = queued.Concat(stored.Select(m => m.MatchId)).ToHashSet(StringComparer.Ordinal);
            foreach (var id in ids.Where(id => !known.Contains(id)))
            {
                db.QueuedMatches.Add(new QueuedMatch { MatchId = id, SourceTier = player.Tier, Status = QueueStatus.Pending, EnqueuedAt = now });
                enqueued++;
            }

            player.LastCrawledAt = now;
            await db.SaveChangesAsync(ct);
        }

        db.ChangeTracker.Clear();
        LogCrawled(logger, players.Count, enqueued);
        return enqueued;
    }

    /// <summary>Télécharge un lot de matchs en attente. Renvoie le nombre de matchs stockés.</summary>
    public async Task<int> FetchAsync(CancellationToken ct)
    {
        var batch = await db.QueuedMatches
            .Where(q => q.Status == QueueStatus.Pending)
            .OrderBy(q => q.EnqueuedAt)
            .Take(Options.FetchBatchSize)
            .ToListAsync(ct);

        var stored = 0;
        foreach (var item in batch)
        {
            try
            {
                if (await db.Matches.AnyAsync(m => m.MatchId == item.MatchId, ct))
                {
                    MarkDone(item);
                }
                else if (await riot.GetMatchAsync(Options.Platform.ToRegional(), item.MatchId, ct) is { } dto)
                {
                    var match = MatchMapper.ToEntity(dto);
                    match.SourceTier = item.SourceTier;
                    db.Matches.Add(match);
                    MarkDone(item);
                    stored++;
                }
                else
                {
                    item.Status = QueueStatus.Failed; // 404 : match introuvable, inutile d'insister
                }

                await db.SaveChangesAsync(ct);
            }
            catch (RiotApiException ex) when (ex is not RiotApiAuthException)
            {
                db.ChangeTracker.Clear();
                db.QueuedMatches.Attach(item);
                item.Attempts++;
                item.Status = item.Attempts >= Options.MaxAttempts ? QueueStatus.Failed : QueueStatus.Pending;
                await db.SaveChangesAsync(ct);
                LogFetchFailed(logger, ex, item.MatchId, item.Attempts);
            }
        }

        db.ChangeTracker.Clear();
        return stored;
    }

    /// <summary>Recalcule les agrégats des patchs les plus récents collectés.</summary>
    public async Task<int> AggregateAsync(CancellationToken ct)
    {
        var patches = await db.Matches
            .Where(m => m.SourceTier != null && m.QueueId == Queues.RankedSolo && m.PatchMajor > 0)
            .Select(m => new { m.PatchMajor, m.PatchMinor })
            .Distinct()
            .ToListAsync(ct);

        var total = 0;
        foreach (var p in patches.Select(p => new Patch(p.PatchMajor, p.PatchMinor)).OrderDescending().Take(Options.PatchesToAggregate))
        {
            total += await aggregator.AggregateAsync(p, ct);
        }

        return total;
    }

    private void MarkDone(QueuedMatch item)
    {
        item.Status = QueueStatus.Done;
        item.ProcessedAt = time.GetUtcNow();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Ladders importés : {Players} joueurs")]
    private static partial void LogSeeded(ILogger logger, int players);

    [LoggerMessage(Level = LogLevel.Information, Message = "Crawl : {Players} joueurs parcourus, {Enqueued} matchs mis en file")]
    private static partial void LogCrawled(ILogger logger, int players, int enqueued);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Échec du téléchargement de {MatchId} (tentative {Attempt})")]
    private static partial void LogFetchFailed(ILogger logger, Exception ex, string matchId, int attempt);
}
