using RiftLens.Infrastructure.Riot.Dtos;
using RiftLens.Infrastructure.StaticData;

namespace RiftLens.Infrastructure.Matches;

/// <summary>Extrait d'une timeline les objets de départ, l'ordre d'achat et l'ordre des sorts d'un joueur.</summary>
public static class TimelineAnalyzer
{
    /// <summary>Achats considérés comme « de départ » (avant la sortie de base).</summary>
    public static readonly TimeSpan StarterWindow = TimeSpan.FromSeconds(90);
    public const int ItemOrderLength = 5;
    public const int SkillOrderLength = 15;
    private const string Skills = "QWER";

    public sealed record Result(string? StarterKey, IReadOnlyList<int> ItemOrder, string? SkillOrder);

    public static Result Analyze(TimelineDto timeline, int participantId, ItemCategories items)
    {
        var events = timeline.Info.Frames
            .SelectMany(f => f.Events ?? [])
            .Where(e => e.ParticipantId == participantId)
            .OrderBy(e => e.Timestamp)
            .ToList();

        // Achats nets : un « annuler » retire le dernier achat de l'objet concerné.
        var purchases = new List<(int Item, long At)>();
        var skills = new List<char>();
        foreach (var e in events)
        {
            switch (e.Type)
            {
                case "ITEM_PURCHASED" when e.ItemId > 0:
                    purchases.Add((e.ItemId, e.Timestamp));
                    break;
                case "ITEM_UNDO" when e.BeforeId > 0:
                    var index = purchases.FindLastIndex(p => p.Item == e.BeforeId);
                    if (index >= 0) { purchases.RemoveAt(index); }
                    break;
                case "SKILL_LEVEL_UP" when e.SkillSlot is >= 1 and <= 4 && e.LevelUpType is null or "NORMAL":
                    skills.Add(Skills[e.SkillSlot - 1]);
                    break;
            }
        }

        var starter = purchases
            .Where(p => p.At <= StarterWindow.TotalMilliseconds && !items.Trinkets.Contains(p.Item))
            .Select(p => p.Item)
            .Order()
            .ToList();

        var order = purchases
            .Select(p => p.Item)
            .Where(items.IsCompleted)
            .Distinct()
            .Take(ItemOrderLength)
            .ToList();

        return new Result(
            starter.Count == 0 ? null : string.Join('-', starter),
            order,
            skills.Count >= 6 ? new string(skills.Take(SkillOrderLength).ToArray()) : null);
    }

    /// <summary>Priorité de montée parmi Q, W, E : ordre dans lequel chaque sort atteint son rang maximal.</summary>
    public static string SkillPriority(string sequence)
    {
        var counts = new Dictionary<char, int> { ['Q'] = 0, ['W'] = 0, ['E'] = 0 };
        var maxedAt = new Dictionary<char, int>();
        for (var i = 0; i < sequence.Length; i++)
        {
            var c = sequence[i];
            if (counts.ContainsKey(c) && ++counts[c] == 5)
            {
                maxedAt[c] = i;
            }
        }

        // Non maxé dans la séquence : départage par le nombre de points mis.
        return string.Join(" > ", counts.Keys
            .OrderBy(c => maxedAt.TryGetValue(c, out var at) ? at : int.MaxValue)
            .ThenByDescending(c => counts[c]));
    }
}
