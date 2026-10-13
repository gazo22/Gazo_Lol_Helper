using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RiftLens.Contracts;
using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Persistence;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.Dtos;

namespace RiftLens.Infrastructure.Players;

internal sealed class PlayerService(
    RiftLensDbContext db,
    IRiotApiClient riot,
    IMatchService matches,
    IMemoryCache cache,
    TimeProvider time) : IPlayerService
{
    /// <summary>Un profil consulté il y a moins de 10 min est servi depuis la base, sans appel Riot.</summary>
    internal static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(10);
    private const int RecentMatches = 20;
    private const int StatsSampleSize = 50;

    public async Task<ProfileDto> GetProfileAsync(Platform platform, RiotId riotId, bool forceRefresh, CancellationToken ct = default)
    {
        var player = await db.Players.SingleOrDefaultAsync(p => p.GameName == riotId.GameName && p.TagLine == riotId.TagLine && p.Platform == platform, ct);
        var now = time.GetUtcNow();
        if (player is null || forceRefresh || now - player.LastRefreshedAt > FreshFor)
        {
            player = await RefreshAsync(platform, riotId, player, now, ct);
        }

        var masteries = await cache.GetOrCreateAsync($"mastery:{player.Puuid}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = FreshFor;
            var list = await riot.GetTopMasteriesAsync(platform, player.Puuid, 5, ct);
            return list.Select(m => new MasteryDto(m.ChampionId, m.ChampionLevel, m.ChampionPoints)).ToList();
        }) ?? [];

        var snapshots = await db.LeagueSnapshots.AsNoTracking()
            .Where(s => s.Puuid == player.Puuid)
            .OrderBy(s => s.CapturedAt)
            .ToListAsync(ct);

        var samples = await LoadSamplesAsync(player.Puuid, ct);

        return new ProfileDto(
            player.Puuid, player.GameName, player.TagLine, player.Platform.ToString(), player.ProfileIconId, player.SummonerLevel,
            LatestRank(snapshots, QueueType.RankedSolo),
            LatestRank(snapshots, QueueType.RankedFlex),
            snapshots.Where(s => s.Queue == QueueType.RankedSolo)
                .Select(s => new LpPointDto(s.CapturedAt, s.Tier.ToString().ToUpperInvariant(), RankMath.FormatDivision(s.Division), s.LeaguePoints, RankMath.LadderScore(s.Tier, s.Division, s.LeaguePoints)))
                .ToList(),
            ProfileStatsCalculator.Compute(samples),
            ProfileStatsCalculator.TopChampions(samples),
            ProfileStatsCalculator.Roles(samples),
            masteries,
            player.LastRefreshedAt);
    }

    private async Task<Player> RefreshAsync(Platform platform, RiotId riotId, Player? player, DateTimeOffset now, CancellationToken ct)
    {
        var account = await riot.GetAccountByRiotIdAsync(platform.ToRegional(), riotId.GameName, riotId.TagLine, ct)
            ?? throw new PlayerNotFoundException($"Joueur {riotId} introuvable.");

        // Le Riot ID peut avoir changé de propriétaire : la vérité est le PUUID.
        if (player is null || player.Puuid != account.Puuid)
        {
            player = await db.Players.SingleOrDefaultAsync(p => p.Puuid == account.Puuid, ct);
        }

        var summoner = await riot.GetSummonerByPuuidAsync(platform, account.Puuid, ct)
            ?? throw new PlayerNotFoundException($"{riotId} n'a pas de compte League of Legends sur {platform}.");
        var entries = await riot.GetLeagueEntriesByPuuidAsync(platform, account.Puuid, ct);

        if (player is null)
        {
            player = new Player { Puuid = account.Puuid, GameName = riotId.GameName, TagLine = riotId.TagLine };
            db.Players.Add(player);
        }

        player.GameName = account.GameName ?? riotId.GameName;
        player.TagLine = account.TagLine ?? riotId.TagLine;
        player.Platform = platform;
        player.ProfileIconId = summoner.ProfileIconId;
        player.SummonerLevel = summoner.SummonerLevel;
        player.LastRefreshedAt = now;

        await AddSnapshotsIfChangedAsync(account.Puuid, entries, now, ct);
        await db.SaveChangesAsync(ct);

        await matches.SyncAsync(platform, account.Puuid, 0, RecentMatches, ct: ct);
        return player;
    }

    private async Task AddSnapshotsIfChangedAsync(string puuid, IReadOnlyList<LeagueEntryDto> entries, DateTimeOffset now, CancellationToken ct)
    {
        foreach (var entry in entries)
        {
            QueueType? queue = entry.QueueType switch
            {
                "RANKED_SOLO_5x5" => QueueType.RankedSolo,
                "RANKED_FLEX_SR" => QueueType.RankedFlex,
                _ => null,
            };
            if (queue is null)
            {
                continue;
            }

            var tier = RankMath.ParseTier(entry.Tier);
            var division = RankMath.ParseDivision(entry.Rank);
            var last = await db.LeagueSnapshots.Where(s => s.Puuid == puuid && s.Queue == queue)
                .OrderByDescending(s => s.CapturedAt).FirstOrDefaultAsync(ct);
            if (last is not null && last.Tier == tier && last.Division == division && last.LeaguePoints == entry.LeaguePoints
                && last.Wins == entry.Wins && last.Losses == entry.Losses)
            {
                continue;
            }

            db.LeagueSnapshots.Add(new LeagueSnapshot
            {
                Puuid = puuid, Queue = queue.Value, Tier = tier, Division = division, LeaguePoints = entry.LeaguePoints,
                Wins = entry.Wins, Losses = entry.Losses, CapturedAt = now,
            });
        }
    }

    private async Task<List<ProfileStatsCalculator.Sample>> LoadSamplesAsync(string puuid, CancellationToken ct)
    {
        var queues = ProfileStatsCalculator.RiftQueues.ToList();
        var recent = await db.Matches.AsNoTracking()
            .Include(m => m.Participants)
            .Where(m => queues.Contains(m.QueueId) && m.Participants.Any(p => p.Puuid == puuid))
            .OrderByDescending(m => m.GameStart)
            .Take(StatsSampleSize)
            .AsSplitQuery()
            .ToListAsync(ct);
        return recent.Select(m => new ProfileStatsCalculator.Sample(m, m.Participants.First(p => p.Puuid == puuid))).ToList();
    }

    private static RankDto? LatestRank(List<LeagueSnapshot> snapshots, QueueType queue) =>
        snapshots.LastOrDefault(s => s.Queue == queue) is { } s
            ? new RankDto(queue.ToString(), s.Tier.ToString().ToUpperInvariant(), RankMath.FormatDivision(s.Division), s.LeaguePoints, s.Wins, s.Losses, HotStreak: false)
            : null;
}
