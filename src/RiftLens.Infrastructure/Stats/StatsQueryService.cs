using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RiftLens.Contracts;
using RiftLens.Domain.Scoring;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Persistence;

namespace RiftLens.Infrastructure.Stats;

public interface IStatsQueryService
{
    Task<IReadOnlyList<string>> GetPatchesAsync(CancellationToken ct = default);

    Task<TierListDto?> GetTierListAsync(string? patch, string? role, string? elo, CancellationToken ct = default);

    Task<ChampionDetailDto?> GetChampionAsync(int championId, string? patch, string? role, string? elo, CancellationToken ct = default);

    Task<MatchupListDto?> GetMatchupsAsync(int championId, string? patch, string? role, string? elo, CancellationToken ct = default);
}

internal sealed class StatsQueryService(RiftLensDbContext db, IOptions<StatsOptions> options) : IStatsQueryService
{
    private StatsOptions Options => options.Value;

    public async Task<IReadOnlyList<string>> GetPatchesAsync(CancellationToken ct = default)
    {
        var patches = await db.PatchTotals.AsNoTracking().Select(t => t.Patch).Distinct().ToListAsync(ct);
        return patches
            .Select(p => (Raw: p, Ok: Patch.TryParse(p, out var parsed), Parsed: parsed))
            .Where(p => p.Ok)
            .OrderByDescending(p => p.Parsed!.Value)
            .Select(p => p.Raw)
            .ToList();
    }

    public async Task<TierListDto?> GetTierListAsync(string? patch, string? role, string? elo, CancellationToken ct = default)
    {
        var context = await ResolveAsync(patch, elo, ct);
        if (context is null)
        {
            return null;
        }

        var (key, buckets, eloKey, total) = context.Value;
        var rows = await ComputeRowsAsync(key, buckets, total, ct);
        if (ParseRole(role) is { } r)
        {
            rows = rows.Where(x => x.Role == r.ToString()).ToList();
        }

        return new TierListDto(key, eloKey, total, Options.MinGames, rows.OrderByDescending(x => x.Score).ToList());
    }

    public async Task<ChampionDetailDto?> GetChampionAsync(int championId, string? patch, string? role, string? elo, CancellationToken ct = default)
    {
        var context = await ResolveAsync(patch, elo, ct);
        if (context is null)
        {
            return null;
        }

        var (key, buckets, eloKey, total) = context.Value;
        var roleStats = await db.ChampionStats.AsNoTracking()
            .Where(s => s.Patch == key && buckets.Contains(s.Bucket) && s.ChampionId == championId)
            .GroupBy(s => s.Role)
            .Select(g => new { Role = g.Key, Games = g.Sum(x => x.Games), Wins = g.Sum(x => x.Wins) })
            .ToListAsync(ct);
        if (roleStats.Count == 0)
        {
            return null;
        }

        var selected = ParseRole(role) is { } requested && roleStats.Any(r => r.Role == requested)
            ? requested
            : roleStats.MaxBy(r => r.Games)!.Role;

        var rows = await ComputeRowsAsync(key, buckets, total, ct);
        var summary = rows.FirstOrDefault(r => r.ChampionId == championId && r.Role == selected.ToString());

        var builds = (await db.BuildStats.AsNoTracking()
                .Where(s => s.Patch == key && buckets.Contains(s.Bucket) && s.ChampionId == championId && s.Role == selected)
                .GroupBy(s => s.ItemsKey)
                .Select(g => new { g.Key, Games = g.Sum(x => x.Games), Wins = g.Sum(x => x.Wins) })
                .ToListAsync(ct))
            .Where(b => b.Games >= Options.MinBuildGames)
            .OrderByDescending(b => b.Games).Take(5)
            .Select(b => new BuildDto(ParseInts(b.Key, '-'), b.Games, b.Wins))
            .ToList();

        var runes = (await db.RuneStats.AsNoTracking()
                .Where(s => s.Patch == key && buckets.Contains(s.Bucket) && s.ChampionId == championId && s.Role == selected)
                .GroupBy(s => s.PerksKey)
                .Select(g => new { g.Key, Games = g.Sum(x => x.Games), Wins = g.Sum(x => x.Wins) })
                .ToListAsync(ct))
            .Where(r => r.Games >= Options.MinBuildGames)
            .OrderByDescending(r => r.Games).Take(3)
            .Select(r => ToRunePage(r.Key, r.Games, r.Wins))
            .ToList();

        var matchups = (await db.MatchupStats.AsNoTracking()
                .Where(s => s.Patch == key && buckets.Contains(s.Bucket) && s.ChampionId == championId && s.Role == selected)
                .GroupBy(s => s.OpponentId)
                .Select(g => new { Opponent = g.Key, Games = g.Sum(x => x.Games), Wins = g.Sum(x => x.Wins) })
                .ToListAsync(ct))
            .Where(m => m.Games >= Options.MinMatchupGames)
            .Select(m => new MatchupDto(m.Opponent, m.Games, m.Wins))
            .ToList();

        // Classement par borne de Wilson : un 70 % sur 20 parties ne passe pas devant un 58 % sur 400.
        var best = matchups.OrderByDescending(m => Wilson.LowerBound(m.Wins, m.Games)).Take(5).ToList();
        var worst = matchups.OrderBy(m => Wilson.Interval(m.Wins, m.Games).Upper).Take(5).ToList();

        return new ChampionDetailDto(
            championId, key, eloKey, selected.ToString(), total, summary,
            roleStats.OrderByDescending(r => r.Games).Select(r => new RoleShareDto(r.Role.ToString(), r.Games, r.Wins)).ToList(),
            builds, runes, best, worst, Options.MinBuildGames, Options.MinMatchupGames);
    }

    public async Task<MatchupListDto?> GetMatchupsAsync(int championId, string? patch, string? role, string? elo, CancellationToken ct = default)
    {
        var context = await ResolveAsync(patch, elo, ct);
        if (context is null)
        {
            return null;
        }

        var (key, buckets, eloKey, _) = context.Value;
        var query = db.MatchupStats.AsNoTracking().Where(s => s.Patch == key && buckets.Contains(s.Bucket) && s.ChampionId == championId);
        var selected = ParseRole(role);
        if (selected is null)
        {
            // Rôle non précisé : celui où le champion est le plus joué.
            var main = await db.ChampionStats.AsNoTracking()
                .Where(s => s.Patch == key && buckets.Contains(s.Bucket) && s.ChampionId == championId)
                .GroupBy(s => s.Role).Select(g => new { Role = g.Key, Games = g.Sum(x => x.Games) })
                .OrderByDescending(g => g.Games).FirstOrDefaultAsync(ct);
            if (main is null)
            {
                return null;
            }

            selected = main.Role;
        }

        var r = selected.Value;
        var matchups = await query.Where(s => s.Role == r)
            .GroupBy(s => s.OpponentId)
            .Select(g => new MatchupDto(g.Key, g.Sum(x => x.Games), g.Sum(x => x.Wins)))
            .ToListAsync(ct);
        return new MatchupListDto(championId, r.ToString(), key, eloKey, Options.MinMatchupGames, matchups.OrderByDescending(m => m.Games).ToList());
    }

    private async Task<List<TierListRowDto>> ComputeRowsAsync(string key, List<RankTier> buckets, int total, CancellationToken ct)
    {
        var stats = await db.ChampionStats.AsNoTracking()
            .Where(s => s.Patch == key && buckets.Contains(s.Bucket))
            .GroupBy(s => new { s.ChampionId, s.Role })
            .Select(g => new { g.Key.ChampionId, g.Key.Role, Games = g.Sum(x => x.Games), Wins = g.Sum(x => x.Wins) })
            .ToListAsync(ct);
        var bans = await db.ChampionBanStats.AsNoTracking()
            .Where(s => s.Patch == key && buckets.Contains(s.Bucket))
            .GroupBy(s => s.ChampionId)
            .Select(g => new { ChampionId = g.Key, Bans = g.Sum(x => x.Bans) })
            .ToDictionaryAsync(b => b.ChampionId, b => b.Bans, ct);

        var visible = stats.Where(s => (double)s.Games / total >= Options.MinPickrate || s.Games >= Options.MinGames).ToList();
        var rows = new List<TierListRowDto>();
        foreach (var byRole in visible.GroupBy(s => s.Role))
        {
            var grades = TierScorer.Grade(byRole.Select(s => new TierInput<int>(s.ChampionId, s.Games, s.Wins, (double)s.Games / total)), Options.MinGames);
            rows.AddRange(byRole.Select(s =>
            {
                var g = grades[s.ChampionId];
                return new TierListRowDto(s.ChampionId, s.Role.ToString(), TierScorer.Label(g.Grade), Math.Round(g.Score, 4), s.Games, s.Wins,
                    (double)s.Games / total, (double)bans.GetValueOrDefault(s.ChampionId) / total, g.SampleTooSmall);
            }));
        }

        return rows;
    }

    private async Task<(string Patch, List<RankTier> Buckets, string Elo, int Total)?> ResolveAsync(string? patch, string? elo, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(patch) ? (await GetPatchesAsync(ct)) is [var latest, ..] ? latest : null : patch;
        if (key is null)
        {
            return null;
        }

        var (eloKey, minTier) = elo?.ToLowerInvariant() switch
        {
            EloFilters.Challenger => (EloFilters.Challenger, RankTier.Challenger),
            EloFilters.GrandmasterPlus => (EloFilters.GrandmasterPlus, RankTier.Grandmaster),
            _ => (EloFilters.MasterPlus, RankTier.Master),
        };
        var buckets = Enum.GetValues<RankTier>().Where(t => t >= minTier).ToList();
        var total = await db.PatchTotals.AsNoTracking().Where(t => t.Patch == key && buckets.Contains(t.Bucket)).SumAsync(t => t.Matches, ct);
        return total == 0 ? null : (key, buckets, eloKey, total);
    }

    private static Role? ParseRole(string? role) =>
        Enum.TryParse<Role>(role, ignoreCase: true, out var r) && r != Role.Unknown ? r : null;

    private static int[] ParseInts(string value, char separator) =>
        value.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(v => int.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

    private static RunePageDto ToRunePage(string key, int games, int wins)
    {
        var parts = key.Split(':');
        var styles = ParseInts(parts[0], '-');
        return new RunePageDto(styles[0], styles.ElementAtOrDefault(1), ParseInts(parts.ElementAtOrDefault(1) ?? string.Empty, '-'), games, wins);
    }
}
