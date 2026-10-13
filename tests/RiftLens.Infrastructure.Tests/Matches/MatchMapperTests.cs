using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Tests.Support;

namespace RiftLens.Infrastructure.Tests.Matches;

public class MatchMapperTests
{
    [Fact]
    public void Mappe_un_match_classe_complet()
    {
        var match = MatchMapper.ToEntity(Fixture.Match());

        Assert.Equal("EUW1_7512345678", match.MatchId);
        Assert.Equal(420, match.QueueId);
        Assert.Equal((15, 20), (match.PatchMajor, match.PatchMinor));
        Assert.Equal(TimeSpan.FromSeconds(1874), match.Duration);
        Assert.False(match.IsRemake);
        Assert.Equal(10, match.Participants.Count);
        Assert.Equal(9, match.Bans.Count); // le ban -1 (aucun) est ignoré
    }

    [Fact]
    public void Mappe_runes_items_et_role()
    {
        var ahri = MatchMapper.ToEntity(Fixture.Match()).Participants.Single(p => p.ChampionId == 103);

        Assert.Equal(Role.Middle, ahri.Role);
        Assert.Equal(3157, ahri.Items[0]);
        Assert.Equal(7, ahri.Items.Length);
        Assert.Equal([4, 14], ahri.Summoners);
        Assert.Equal((8100, 8200), (ahri.PrimaryStyle, ahri.SubStyle));
        Assert.Equal([8112, 8139, 8138, 8135, 8226, 8210, 5005, 5008, 5011], ahri.Perks);
        Assert.Equal("Gazo", ahri.RiotIdGameName);
    }

    [Fact]
    public void Une_reddition_precoce_courte_est_un_remake()
    {
        var dto = Fixture.Match();
        var info = dto.Info with
        {
            GameDuration = 200,
            Participants = dto.Info.Participants.Select(p => p with { GameEndedInEarlySurrender = true }).ToList(),
        };

        Assert.True(MatchMapper.ToEntity(dto with { Info = info }).IsRemake);
    }
}
