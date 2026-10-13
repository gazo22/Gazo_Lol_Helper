using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Players;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.Players;

public sealed class PlayerServiceTests : IDisposable
{
    private readonly SqliteDb _db = new();
    private readonly FixtureRiotClient _riot = new();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-10-13T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    private PlayerService Create()
    {
        var ctx = _db.Create();
        return new PlayerService(ctx, _riot, new MatchService(ctx, _riot, NullLogger<MatchService>.Instance), _cache, _time);
    }

    [Fact]
    public async Task Construit_le_profil_complet()
    {
        var profile = await Create().GetProfileAsync(Platform.Euw1, new RiotId("Gazo", "EUW"), forceRefresh: false);

        Assert.Equal("Gazo", profile.GameName);
        Assert.Equal(412, profile.SummonerLevel);
        Assert.Equal(("EMERALD", "II", 64), (profile.Solo!.Tier, profile.Solo.Division, profile.Solo.LeaguePoints));
        Assert.Equal("PLATINUM", profile.Flex!.Tier);
        Assert.Single(profile.LpHistory);
        Assert.Equal(3, profile.Masteries.Count);
        Assert.Equal(1, profile.Stats.Games); // fixture : le joueur (puuid #1) joue Aatrox
        Assert.Equal(266, profile.TopChampions.Single().ChampionId);
    }

    [Fact]
    public async Task Un_profil_recent_est_servi_sans_appel_Riot()
    {
        await Create().GetProfileAsync(Platform.Euw1, new RiotId("Gazo", "EUW"), false);
        _time.Advance(TimeSpan.FromMinutes(5));
        await Create().GetProfileAsync(Platform.Euw1, new RiotId("Gazo", "EUW"), false);
        Assert.Equal(1, _riot.AccountCalls);

        _time.Advance(TimeSpan.FromMinutes(6));
        await Create().GetProfileAsync(Platform.Euw1, new RiotId("Gazo", "EUW"), false);
        Assert.Equal(2, _riot.AccountCalls);
    }

    [Fact]
    public async Task Un_rang_inchange_ne_cree_pas_de_nouveau_point_LP()
    {
        await Create().GetProfileAsync(Platform.Euw1, new RiotId("Gazo", "EUW"), false);
        var profile = await Create().GetProfileAsync(Platform.Euw1, new RiotId("Gazo", "EUW"), forceRefresh: true);
        Assert.Single(profile.LpHistory);
    }

    [Fact]
    public async Task Riot_id_inconnu_leve_PlayerNotFound() =>
        await Assert.ThrowsAsync<PlayerNotFoundException>(() => Create().GetProfileAsync(Platform.Euw1, new RiotId("Personne", "EUW"), false));

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }
}
