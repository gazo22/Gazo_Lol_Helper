using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Stats;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.Stats;

public sealed class StatsTests : IDisposable
{
    private static readonly Patch Patch = new(15, 20);
    private readonly SqliteDb _db = new();
    private readonly FakeTimeProvider _time = new();

    private StatsAggregator Aggregator() => new(_db.Create(), new FixtureStaticData(), _time, NullLogger<StatsAggregator>.Instance);

    private StatsQueryService Query(int minGames = 1, int minBuild = 1, int minMatchup = 1) =>
        new(_db.Create(), Options.Create(new StatsOptions { MinGames = minGames, MinBuildGames = minBuild, MinMatchupGames = minMatchup, MinPickrate = 0 }));

    /// <summary>Insère n copies du match fixture (bleu gagne) ; si <paramref name="flip"/>, les rouges gagnent.</summary>
    private async Task SeedAsync(int count, RankTier tier, int offset = 0, bool flip = false)
    {
        await using var ctx = _db.Create();
        for (var i = 0; i < count; i++)
        {
            var dto = Fixture.Match();
            dto = dto with
            {
                Metadata = dto.Metadata with { MatchId = $"EUW1_{offset + i:D6}" },
                Info = dto.Info with { Participants = dto.Info.Participants.Select(p => p with { Win = flip ? p.TeamId == 200 : p.TeamId == 100 }).ToList() },
            };
            var match = MatchMapper.ToEntity(dto);
            match.SourceTier = tier;
            ctx.Matches.Add(match);
        }

        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task Agrege_winrates_matchups_builds_et_runes()
    {
        await SeedAsync(3, RankTier.Challenger);
        await SeedAsync(1, RankTier.Master, offset: 100, flip: true);

        Assert.Equal(4, await Aggregator().AggregateAsync(Patch));

        await using var ctx = _db.Create();
        var ahri = await ctx.ChampionStats.Where(s => s.ChampionId == 103 && s.Role == Role.Middle).ToListAsync();
        Assert.Equal(4, ahri.Sum(s => s.Games));
        Assert.Equal(3, ahri.Sum(s => s.Wins));

        var mu = await ctx.MatchupStats.Where(s => s.ChampionId == 103 && s.OpponentId == 517).SumAsync(s => s.Games);
        Assert.Equal(4, mu); // Ahri vs Sylas, mid

        var build = await ctx.BuildStats.SingleAsync(s => s.ChampionId == 103 && s.Bucket == RankTier.Challenger);
        Assert.Equal("3157-4645-3089", build.ItemsKey);
        Assert.Equal(9, await ctx.ChampionBanStats.CountAsync(s => s.Bucket == RankTier.Challenger));
    }

    [Fact]
    public async Task L_agregation_est_idempotente()
    {
        await SeedAsync(2, RankTier.Challenger);
        await Aggregator().AggregateAsync(Patch);
        await Aggregator().AggregateAsync(Patch);

        await using var ctx = _db.Create();
        Assert.Equal(2, await ctx.ChampionStats.Where(s => s.ChampionId == 103).SumAsync(s => s.Games));
        Assert.Equal(2, await ctx.PatchTotals.SumAsync(t => t.Matches));
    }

    [Fact]
    public async Task Les_matchs_hors_collector_ou_remakes_sont_ignores()
    {
        await SeedAsync(1, RankTier.Challenger);
        await using (var ctx = _db.Create())
        {
            var m = await ctx.Matches.SingleAsync();
            m.SourceTier = null;
            await ctx.SaveChangesAsync();
        }

        Assert.Equal(0, await Aggregator().AggregateAsync(Patch));
    }

    [Fact]
    public async Task Tier_list_filtre_par_elo_et_role()
    {
        await SeedAsync(3, RankTier.Challenger);
        await SeedAsync(2, RankTier.Master, offset: 100, flip: true);
        await Aggregator().AggregateAsync(Patch);

        var all = await Query().GetTierListAsync(null, null, "master");
        Assert.Equal(("15.20", 5), (all!.Patch, all.TotalMatches));
        Assert.Equal(10, all.Rows.Count);

        var chall = await Query().GetTierListAsync("15.20", "middle", "challenger");
        var ahri = Assert.Single(chall!.Rows, r => r.ChampionId == 103);
        Assert.Equal((3, 3, 1.0), (ahri.Games, ahri.Wins, ahri.Pickrate));
        Assert.All(chall.Rows, r => Assert.Equal("Middle", r.Role));
        Assert.Equal(1.0, chall.Rows.Single(r => r.ChampionId == 103).Winrate);
        Assert.Equal(0.0, chall.Rows.Single(r => r.ChampionId == 517).Winrate);
    }

    [Fact]
    public async Task Detail_champion_avec_builds_runes_et_matchups()
    {
        await SeedAsync(4, RankTier.Grandmaster);
        await Aggregator().AggregateAsync(Patch);

        var detail = await Query().GetChampionAsync(103, null, null, null);

        Assert.NotNull(detail);
        Assert.Equal("Middle", detail.Role);
        Assert.Equal([3157, 4645, 3089], Assert.Single(detail.Builds).Items);
        var runes = Assert.Single(detail.RunePages);
        Assert.Equal((8100, 8200), (runes.PrimaryStyle, runes.SubStyle));
        Assert.Equal(9, runes.Perks.Count);
        Assert.Equal(517, Assert.Single(detail.BestMatchups).OpponentId);
        Assert.NotNull(detail.Summary);
    }

    [Fact]
    public async Task Les_seuils_d_echantillon_masquent_builds_et_matchups()
    {
        await SeedAsync(4, RankTier.Grandmaster);
        await Aggregator().AggregateAsync(Patch);

        var detail = await Query(minGames: 150, minBuild: 30, minMatchup: 20).GetChampionAsync(103, null, null, null);

        Assert.Empty(detail!.Builds);
        Assert.Empty(detail.BestMatchups);
        Assert.True(detail.Summary!.SampleTooSmall);
    }

    [Fact]
    public async Task Liste_complete_des_matchups_dans_le_role_principal()
    {
        await SeedAsync(3, RankTier.Master);
        await Aggregator().AggregateAsync(Patch);

        var list = await Query(minMatchup: 20).GetMatchupsAsync(103, null, null, null);

        Assert.Equal("Middle", list!.Role);
        var sylas = Assert.Single(list.Matchups);
        Assert.Equal((517, 3, 3), (sylas.OpponentId, sylas.Games, sylas.Wins));
        Assert.Equal(20, list.MinGames); // les faibles échantillons sont renvoyés, l'UI les signale
    }

    [Fact]
    public async Task Sans_donnees_la_tier_list_est_null() => Assert.Null(await Query().GetTierListAsync(null, null, null));

    public void Dispose() => _db.Dispose();
}
