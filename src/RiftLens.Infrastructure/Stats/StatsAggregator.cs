using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RiftLens.Contracts;
using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Persistence;
using RiftLens.Infrastructure.StaticData;

namespace RiftLens.Infrastructure.Stats;

public interface IStatsAggregator
{
    /// <summary>Recalcule entièrement les agrégats d'un patch (idempotent). Renvoie le nombre de matchs agrégés.</summary>
    Task<int> AggregateAsync(Patch patch, CancellationToken ct = default);
}

/// <summary>
/// Agrégation en mémoire, par lots, des matchs classés solo collectés (<see cref="Match.SourceTier"/> renseigné).
/// Les lignes du patch sont ensuite remplacées en une transaction.
/// </summary>
internal sealed partial class StatsAggregator(
    RiftLensDbContext db,
    IStaticDataService staticData,
    TimeProvider time,
    ILogger<StatsAggregator> logger) : IStatsAggregator
{
    private const int BatchSize = 200; // les timelines compressées alourdissent chaque match

    public async Task<int> AggregateAsync(Patch patch, CancellationToken ct = default)
    {
        var categories = await staticData.GetItemCategoriesAsync(ct);
        var key = patch.ToString();
        var acc = new Accumulator(key);

        string? lastId = null;
        while (true)
        {
            var query = db.Matches.AsNoTracking()
                .Where(m => m.PatchMajor == patch.Major && m.PatchMinor == patch.Minor
                    && m.QueueId == Queues.RankedSolo && m.SourceTier != null && !m.IsRemake);
            if (lastId is not null)
            {
#pragma warning disable CA1309 // seule forme traduite en SQL par EF Core ; colonne non Unicode, tri binaire suffisant
                query = query.Where(m => string.Compare(m.MatchId, lastId) > 0);
#pragma warning restore CA1309
            }

            var batch = await query.OrderBy(m => m.MatchId).Take(BatchSize)
                .Include(m => m.Participants).Include(m => m.Bans).AsSplitQuery().ToListAsync(ct);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var match in batch)
            {
                acc.Add(match, categories);
            }

            lastId = batch[^1].MatchId;
        }

        // La stratégie de reprise SQL Server impose d'exécuter la transaction comme un bloc rejouable.
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async token =>
        {
            db.ChangeTracker.Clear();
            await using var tx = await db.Database.BeginTransactionAsync(token);
            await db.PatchTotals.Where(s => s.Patch == key).ExecuteDeleteAsync(token);
            await db.ChampionStats.Where(s => s.Patch == key).ExecuteDeleteAsync(token);
            await db.ChampionBanStats.Where(s => s.Patch == key).ExecuteDeleteAsync(token);
            await db.MatchupStats.Where(s => s.Patch == key).ExecuteDeleteAsync(token);
            await db.BuildStats.Where(s => s.Patch == key).ExecuteDeleteAsync(token);
            await db.RuneStats.Where(s => s.Patch == key).ExecuteDeleteAsync(token);
            await db.SequenceStats.Where(s => s.Patch == key).ExecuteDeleteAsync(token);

            var now = time.GetUtcNow();
            db.PatchTotals.AddRange(acc.Totals.Select(t => new PatchTotals { Patch = key, Bucket = t.Key, Matches = t.Value, ComputedAt = now }));
            db.ChampionStats.AddRange(acc.Champions.Values);
            db.ChampionBanStats.AddRange(acc.Bans.Values);
            db.MatchupStats.AddRange(acc.Matchups.Values);
            db.BuildStats.AddRange(acc.Builds.Values);
            db.RuneStats.AddRange(acc.Runes.Values);
            db.SequenceStats.AddRange(acc.Sequences.Values);
            await db.SaveChangesAsync(token);
            await tx.CommitAsync(token);
        }, ct);
        db.ChangeTracker.Clear();

        var total = acc.Totals.Values.Sum();
        LogAggregated(logger, key, total, acc.Champions.Count);
        return total;
    }

    internal static string RuneKey(MatchParticipant p) => $"{p.PrimaryStyle}-{p.SubStyle}:{string.Join('-', p.Perks)}";

    private sealed class Accumulator(string patch)
    {
        public Dictionary<RankTier, int> Totals { get; } = [];
        public Dictionary<(RankTier, int, Role), ChampionStat> Champions { get; } = [];
        public Dictionary<(RankTier, int), ChampionBanStat> Bans { get; } = [];
        public Dictionary<(RankTier, Role, int, int), MatchupStat> Matchups { get; } = [];
        public Dictionary<(RankTier, int, Role, string), BuildStat> Builds { get; } = [];
        public Dictionary<(RankTier, int, Role, string), RuneStat> Runes { get; } = [];
        public Dictionary<(RankTier, int, Role, SequenceKind, string), SequenceStat> Sequences { get; } = [];

        public void Add(Match match, ItemCategories categories)
        {
            var bucket = match.SourceTier!.Value;
            Totals[bucket] = Totals.GetValueOrDefault(bucket) + 1;
            var timeline = match.TimelineGzip is { } gz ? MatchService.Decompress(gz) : null;

            foreach (var ban in match.Bans.Select(b => b.ChampionId).Distinct())
            {
                Get(Bans, (bucket, ban), () => new ChampionBanStat { Patch = patch, Bucket = bucket, ChampionId = ban }).Bans++;
            }

            foreach (var p in match.Participants.Where(p => p.Role != Role.Unknown))
            {
                var win = p.Win ? 1 : 0;
                var champ = Get(Champions, (bucket, p.ChampionId, p.Role), () => new ChampionStat { Patch = patch, Bucket = bucket, ChampionId = p.ChampionId, Role = p.Role });
                champ.Games++;
                champ.Wins += win;

                var opponent = match.Participants.FirstOrDefault(o => o.TeamId != p.TeamId && o.Role == p.Role);
                if (opponent is not null)
                {
                    var mu = Get(Matchups, (bucket, p.Role, p.ChampionId, opponent.ChampionId), () => new MatchupStat { Patch = patch, Bucket = bucket, Role = p.Role, ChampionId = p.ChampionId, OpponentId = opponent.ChampionId });
                    mu.Games++;
                    mu.Wins += win;
                }

                if (categories.CoreBuildKey(p.Items) is { } itemsKey)
                {
                    var build = Get(Builds, (bucket, p.ChampionId, p.Role, itemsKey), () => new BuildStat { Patch = patch, Bucket = bucket, ChampionId = p.ChampionId, Role = p.Role, ItemsKey = itemsKey });
                    build.Games++;
                    build.Wins += win;
                }

                if (p.Summoners is [var s1, var s2] && s1 > 0 && s2 > 0)
                {
                    AddSequence(bucket, p, SequenceKind.SummonerSpells, $"{Math.Min(s1, s2)}-{Math.Max(s1, s2)}", win);
                }

                if (timeline is not null)
                {
                    var t = TimelineAnalyzer.Analyze(timeline, p.ParticipantId, categories);
                    AddSequence(bucket, p, SequenceKind.StarterItems, t.StarterKey, win);
                    AddSequence(bucket, p, SequenceKind.ItemOrder, t.ItemOrder.Count >= 2 ? string.Join('-', t.ItemOrder) : null, win);
                    AddSequence(bucket, p, SequenceKind.SkillOrder, t.SkillOrder, win);
                }

                if (p.PrimaryStyle > 0)
                {
                    var runeKey = RuneKey(p);
                    var rune = Get(Runes, (bucket, p.ChampionId, p.Role, runeKey), () => new RuneStat { Patch = patch, Bucket = bucket, ChampionId = p.ChampionId, Role = p.Role, PerksKey = runeKey });
                    rune.Games++;
                    rune.Wins += win;
                }
            }
        }

        private void AddSequence(RankTier bucket, MatchParticipant p, SequenceKind kind, string? key, int win)
        {
            if (key is null)
            {
                return;
            }

            var stat = Get(Sequences, (bucket, p.ChampionId, p.Role, kind, key), () => new SequenceStat
            {
                Patch = patch, Bucket = bucket, ChampionId = p.ChampionId, Role = p.Role, Kind = kind, Key = key,
            });
            stat.Games++;
            stat.Wins += win;
        }

        private static T Get<TKey, T>(Dictionary<TKey, T> map, TKey key, Func<T> create)
            where TKey : notnull
        {
            if (!map.TryGetValue(key, out var value))
            {
                value = create();
                map[key] = value;
            }

            return value;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agrégation du patch {Patch} : {Matches} matchs, {Rows} lignes champion/rôle")]
    private static partial void LogAggregated(ILogger logger, string patch, int matches, int rows);
}
