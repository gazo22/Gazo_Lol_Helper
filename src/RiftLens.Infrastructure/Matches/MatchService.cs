using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RiftLens.Contracts;
using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Persistence;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.Dtos;
using RiftLens.Infrastructure.Riot.RateLimit;

namespace RiftLens.Infrastructure.Matches;

internal sealed partial class MatchService(RiftLensDbContext db, IRiotApiClient riot, ILogger<MatchService> logger) : IMatchService
{
    public const int MaxPageSize = 50;

    public async Task<int> SyncAsync(Platform platform, string puuid, int start, int count, RequestPriority? priority = null, CancellationToken ct = default)
    {
        var route = platform.ToRegional();
        var ids = await riot.GetMatchIdsAsync(route, puuid, new MatchIdQuery(start, Math.Clamp(count, 1, 100), Priority: priority), ct);
        if (ids.Count == 0)
        {
            return 0;
        }

        // Jamais de re-téléchargement : on ne demande que les matchs absents.
        var known = await db.Matches.Where(m => ids.Contains(m.MatchId)).Select(m => m.MatchId).ToListAsync(ct);
        var missing = ids.Except(known, StringComparer.Ordinal).ToList();
        var stored = 0;
        foreach (var matchId in missing)
        {
            var dto = await riot.GetMatchAsync(route, matchId, ct);
            if (dto is null)
            {
                continue;
            }

            if (await TryStoreAsync(MatchMapper.ToEntity(dto), ct))
            {
                stored++;
            }
        }

        LogSynced(logger, puuid, ids.Count, missing.Count, stored);
        return stored;
    }

    public async Task<IReadOnlyList<MatchSummaryDto>> GetHistoryAsync(Platform platform, string puuid, int start, int count, CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, MaxPageSize);
        var page = await LoadPageAsync(puuid, start, count, ct);
        if (page.Count < count)
        {
            // Pas assez de matchs en base pour cette page : on complète depuis Riot.
            await SyncAsync(platform, puuid, start, count, ct: ct);
            page = await LoadPageAsync(puuid, start, count, ct);
        }

        return page;
    }

    public async Task<MatchDetailDto?> GetDetailAsync(string matchId, CancellationToken ct = default)
    {
        var match = await LoadMatchAsync(matchId, ct);
        if (match is null && MatchIds.TryGetPlatform(matchId, out var platform))
        {
            var dto = await riot.GetMatchAsync(platform.ToRegional(), matchId, ct);
            if (dto is not null)
            {
                await TryStoreAsync(MatchMapper.ToEntity(dto), ct);
                match = await LoadMatchAsync(matchId, ct);
            }
        }

        return match is null ? null : MatchDtoMapper.ToDetail(match);
    }

    public async Task<IReadOnlyList<GoldDiffPointDto>?> GetGoldDiffAsync(string matchId, CancellationToken ct = default)
    {
        var match = await db.Matches.Include(m => m.Participants).SingleOrDefaultAsync(m => m.MatchId == matchId, ct);
        if (match is null || !MatchIds.TryGetPlatform(matchId, out var platform))
        {
            return null;
        }

        TimelineDto? timeline;
        if (match.TimelineGzip is { } cached)
        {
            timeline = Decompress(cached);
        }
        else
        {
            // Timeline téléchargée à la demande puis conservée compressée.
            timeline = await riot.GetTimelineAsync(platform.ToRegional(), matchId, ct);
            if (timeline is null)
            {
                return null;
            }

            match.TimelineGzip = Compress(timeline);
            await db.SaveChangesAsync(ct);
        }

        if (timeline is null)
        {
            return null;
        }

        var teams = match.Participants.ToDictionary(p => p.ParticipantId, p => p.TeamId);
        return GoldDiffCalculator.Compute(timeline, teams);
    }

    private async Task<List<MatchSummaryDto>> LoadPageAsync(string puuid, int start, int count, CancellationToken ct)
    {
        var matches = await db.Matches
            .AsNoTracking()
            .Include(m => m.Participants)
            .Where(m => m.Participants.Any(p => p.Puuid == puuid))
            .OrderByDescending(m => m.GameStart)
            .Skip(start)
            .Take(count)
            .AsSplitQuery()
            .ToListAsync(ct);
        return matches.Select(m => MatchDtoMapper.ToSummary(m, m.Participants.First(p => p.Puuid == puuid))).ToList();
    }

    private Task<Match?> LoadMatchAsync(string matchId, CancellationToken ct) =>
        db.Matches.AsNoTracking().Include(m => m.Participants).Include(m => m.Bans).AsSplitQuery()
            .SingleOrDefaultAsync(m => m.MatchId == matchId, ct);

    private async Task<bool> TryStoreAsync(Match match, CancellationToken ct)
    {
        db.Matches.Add(match);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Stocké entre-temps par une requête concurrente (Api ou Collector) : on ignore.
            db.ChangeTracker.Clear();
            return false;
        }
        finally
        {
            db.Entry(match).State = EntityState.Detached;
        }
    }

    internal static byte[] Compress(TimelineDto timeline)
    {
        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.SmallestSize))
        {
            JsonSerializer.Serialize(gzip, timeline, RiotJsonContext.Default.TimelineDto);
        }

        return buffer.ToArray();
    }

    internal static TimelineDto? Decompress(byte[] data)
    {
        using var gzip = new GZipStream(new MemoryStream(data), CompressionMode.Decompress);
        return JsonSerializer.Deserialize(gzip, RiotJsonContext.Default.TimelineDto);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync {Puuid} : {Ids} ids, {Missing} manquants, {Stored} stockés")]
    private static partial void LogSynced(ILogger logger, string puuid, int ids, int missing, int stored);
}
