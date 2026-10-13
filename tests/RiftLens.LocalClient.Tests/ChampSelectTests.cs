using System.Text.Json;
using RiftLens.LocalClient.ChampSelect;
using RiftLens.LocalClient.Connection;

namespace RiftLens.LocalClient.Tests;

public class ChampSelectTests
{
    private static ChampSelectState Load()
    {
        using var doc = JsonDocument.Parse(Fixture.Read("champ-select-session.json"));
        return ChampSelectMapper.Map(doc.RootElement);
    }

    [Fact]
    public void Picks_intentions_roles_et_joueur_local()
    {
        var s = Load();

        Assert.Equal(2, s.LocalCellId);
        Assert.Equal("BAN_PICK", s.Phase);
        Assert.Equal(5, s.MyTeam.Count);
        Assert.Equal(5, s.TheirTeam.Count);
        Assert.Equal(103, s.LocalChampionId); // intention de pick (survol)
        Assert.Equal("Middle", s.Local!.Role);
        Assert.Equal(["Top", "Jungle", "Middle", "Bottom", "Support"], s.MyTeam.Select(m => m.Role));
        Assert.True(s.MyTeam[0].IsLocked);
        Assert.Equal(64, s.MyTeam[1].DisplayedChampionId);
        Assert.Null(s.TheirTeam[0].Role); // position adverse inconnue
        Assert.Equal(122, s.TheirTeam[0].ChampionId);
        Assert.InRange(s.TimeLeft.TotalSeconds, 27, 28);
    }

    [Fact]
    public void Bans_depuis_les_actions_terminees()
    {
        var s = Load();
        Assert.Equal([157], s.MyBans);
        Assert.Equal([238], s.TheirBans);
    }

    [Fact]
    public void Aucune_information_d_identite_n_est_conservee()
    {
        // Règle Riot : les noms des coéquipiers masqués ne doivent jamais être révélés.
        var json = JsonSerializer.Serialize(Load());

        Assert.DoesNotContain("CoéquipierMasqué", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Gazo", json, StringComparison.Ordinal);
        Assert.DoesNotContain("123456789", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain(typeof(ChampSelectSlot).GetProperties(), p => p.Name.Contains("Name", StringComparison.Ordinal) || p.Name.Contains("Puuid", StringComparison.Ordinal));
    }

    [Fact]
    public void Evenement_Delete_termine_le_champ_select()
    {
        using var service = new LeagueClientService(null!, Microsoft.Extensions.Options.Options.Create(new LocalClientOptions()), TimeProvider.System, Microsoft.Extensions.Logging.Abstractions.NullLogger<LeagueClientService>.Instance);
        var changes = 0;
        service.Changed += () => changes++;

        Assert.True(WampMessage.TryParseEvent(Fixture.Read("wamp-event.json"), out var update));
        service.OnEvent(update);
        Assert.NotNull(service.ChampSelect);

        service.OnEvent(update with { EventType = "Delete", Data = default });
        Assert.Null(service.ChampSelect);
        Assert.Equal(2, changes);
    }

    [Theory]
    [InlineData("utility", "Support")]
    [InlineData("MIDDLE", "Middle")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Positions(string? lcu, string? expected) => Assert.Equal(expected, ChampSelectMapper.MapPosition(lcu));
}
