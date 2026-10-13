using Microsoft.Extensions.Logging.Abstractions;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.Matches;

public sealed class MatchServiceTests : IDisposable
{
    private const string Puuid = "puuid-du-joueur";
    private readonly SqliteDb _db = new();
    private readonly FixtureRiotClient _riot = new() { MatchIds = ["EUW1_3", "EUW1_2", "EUW1_1"] };

    private MatchService Create() => new(_db.Create(), _riot, NullLogger<MatchService>.Instance);

    [Fact]
    public async Task Un_match_deja_stocke_n_est_jamais_retelecharge()
    {
        Assert.Equal(3, await Create().SyncAsync(Platform.Euw1, Puuid, 0, 20));
        Assert.Equal(3, _riot.MatchCalls);

        _riot.MatchIds = ["EUW1_4", "EUW1_3", "EUW1_2", "EUW1_1"];
        Assert.Equal(1, await Create().SyncAsync(Platform.Euw1, Puuid, 0, 20));
        Assert.Equal(4, _riot.MatchCalls);
    }

    [Fact]
    public async Task L_historique_complete_depuis_Riot_puis_sert_la_base()
    {
        var puuid = Fixture.Match().Info.Participants[2].Puuid;

        var page = await Create().GetHistoryAsync(Platform.Euw1, puuid, 0, 2);
        Assert.Equal(2, page.Count);
        Assert.Equal(103, page[0].ChampionId);
        Assert.Equal(10, page[0].Players.Count);
        Assert.InRange(page[0].KillParticipation, 0, 1);

        var calls = _riot.MatchCalls;
        await Create().GetHistoryAsync(Platform.Euw1, puuid, 0, 2);
        Assert.Equal(calls, _riot.MatchCalls);
    }

    [Fact]
    public async Task La_timeline_est_telechargee_une_fois_puis_conservee()
    {
        await Create().SyncAsync(Platform.Euw1, Puuid, 0, 1);

        var first = await Create().GetGoldDiffAsync("EUW1_3");
        var second = await Create().GetGoldDiffAsync("EUW1_3");

        Assert.Equal(32, first!.Count);
        Assert.Equal(first.Select(p => p.Diff), second!.Select(p => p.Diff));
        Assert.Equal(1, _riot.TimelineCalls);
    }

    [Fact]
    public async Task Le_detail_regroupe_les_equipes_et_les_bans()
    {
        var detail = await Create().GetDetailAsync("EUW1_7512345678");

        Assert.NotNull(detail);
        Assert.Equal([100, 200], detail.Teams.Select(t => t.TeamId));
        Assert.True(detail.Teams[0].Win);
        Assert.Equal(5, detail.Teams[0].Players.Count);
        Assert.Equal([157, 238, 84, 350], detail.Teams[0].Bans);
    }

    public void Dispose() => _db.Dispose();
}
