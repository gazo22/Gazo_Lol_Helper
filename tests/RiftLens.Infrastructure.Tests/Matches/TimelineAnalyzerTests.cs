using RiftLens.Infrastructure.Matches;
using RiftLens.Infrastructure.Riot.Dtos;
using RiftLens.Infrastructure.StaticData;

namespace RiftLens.Infrastructure.Tests.Matches;

public sealed class TimelineAnalyzerTests
{
    private static readonly ItemCategories Items = new(
        new HashSet<int> { 3157, 4645, 3089 }, new HashSet<int> { 1001, 3020 }, new HashSet<int> { 3340 });

    internal static TimelineEventDto Buy(long ms, int item, int pid = 3) => new("ITEM_PURCHASED", ms, pid, item, 0, 0, null);
    internal static TimelineEventDto Undo(long ms, int item, int pid = 3) => new("ITEM_UNDO", ms, pid, 0, item, 0, null);
    internal static TimelineEventDto Skill(long ms, int slot, int pid = 3, string type = "NORMAL") => new("SKILL_LEVEL_UP", ms, pid, 0, 0, slot, type);

    internal static TimelineDto Timeline(params TimelineEventDto[] events) =>
        new(new TimelineInfoDto(60_000, [new TimelineFrameDto(0, new Dictionary<string, ParticipantFrameDto>()) { Events = events }]));

    /// <summary>Q W E Q Q R Q E Q E R E E W W — Q max, puis E, puis W.</summary>
    internal static readonly int[] SkillSlots = [1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 3, 3, 2, 2];

    internal static TimelineDto Typical(int pid = 3) => Timeline(
    [
        Buy(10_000, 1056, pid), Buy(11_000, 2003, pid), Buy(12_000, 2003, pid), Buy(13_000, 3340, pid),
        Buy(400_000, 1001, pid), Buy(600_000, 4645, pid), Buy(700_000, 3020, pid), Buy(900_000, 3157, pid),
        Buy(901_000, 3089, pid), Undo(905_000, 3089, pid), Buy(1_200_000, 3089, pid),
        .. SkillSlots.Select((s, i) => Skill(1_000 + (i * 60_000), s, pid)),
        Skill(2_000_000, 4, pid, "EVOLVE"), Buy(5_000, 3157, pid: 7),
    ]);

    [Fact]
    public void Extrait_depart_ordre_d_achat_et_competences()
    {
        var result = TimelineAnalyzer.Analyze(Typical(), 3, Items);

        Assert.Equal("1056-2003-2003", result.StarterKey); // totem exclu
        Assert.Equal([4645, 3020, 3157, 3089], result.ItemOrder); // bottes de base exclues, undo ignoré
        Assert.Equal("QWEQQRQEQEREEWW", result.SkillOrder);
    }

    [Fact]
    public void Sans_assez_de_niveaux_pas_d_ordre_des_sorts()
    {
        var result = TimelineAnalyzer.Analyze(Timeline(Skill(1, 1), Skill(2, 2), Skill(3, 3)), 3, Items);

        Assert.Null(result.SkillOrder);
        Assert.Null(result.StarterKey);
        Assert.Empty(result.ItemOrder);
    }

    [Theory]
    [InlineData("QWEQQRQEQEREEWW", "Q > E > W")]
    [InlineData("QEWEEREWEWRWWQQ", "E > W > Q")]
    [InlineData("QWEWQ", "Q > W > E")]
    public void Priorite_selon_l_ordre_de_maximisation(string sequence, string expected) =>
        Assert.Equal(expected, TimelineAnalyzer.SkillPriority(sequence));
}
