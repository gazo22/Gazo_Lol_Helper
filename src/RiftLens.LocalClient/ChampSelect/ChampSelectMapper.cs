using System.Text.Json;

namespace RiftLens.LocalClient.ChampSelect;

/// <summary>Convertit <c>/lol-champ-select/v1/session</c> en <see cref="ChampSelectState"/>.</summary>
public static class ChampSelectMapper
{
    public static ChampSelectState Map(JsonElement session)
    {
        var localCell = Int(session, "localPlayerCellId", -1);
        var actions = Actions(session).ToList();

        var lockedCells = actions.Where(a => a.Type == "pick" && a.Completed).Select(a => a.ActorCellId).ToHashSet();

        var myBans = new List<int>();
        var theirBans = new List<int>();
        foreach (var ban in actions.Where(a => a.Type == "ban" && a.Completed && a.ChampionId > 0))
        {
            (ban.IsAlly ? myBans : theirBans).Add(ban.ChampionId);
        }

        // Anciens clients / modes : bans exposés directement.
        if (myBans.Count == 0 && theirBans.Count == 0 && session.TryGetProperty("bans", out var bans))
        {
            myBans.AddRange(Ints(bans, "myTeamBans"));
            theirBans.AddRange(Ints(bans, "theirTeamBans"));
        }

        var timer = session.TryGetProperty("timer", out var t) ? t : default;
        return new ChampSelectState(
            Long(session, "gameId"),
            localCell,
            timer.ValueKind == JsonValueKind.Object && timer.TryGetProperty("phase", out var phase) ? phase.GetString() ?? string.Empty : string.Empty,
            TimeSpan.FromMilliseconds(timer.ValueKind == JsonValueKind.Object ? Double(timer, "adjustedTimeLeftInPhase") : 0),
            Team(session, "myTeam", localCell, lockedCells),
            Team(session, "theirTeam", localCell, lockedCells),
            myBans,
            theirBans);
    }

    public static string? MapPosition(string? position) => position?.ToLowerInvariant() switch
    {
        "top" => "Top",
        "jungle" => "Jungle",
        "middle" or "mid" => "Middle",
        "bottom" or "bot" => "Bottom",
        "utility" or "support" => "Support",
        _ => null,
    };

    private static List<ChampSelectSlot> Team(JsonElement session, string property, int localCell, HashSet<int> locked)
    {
        var slots = new List<ChampSelectSlot>();
        if (!session.TryGetProperty(property, out var team) || team.ValueKind != JsonValueKind.Array)
        {
            return slots;
        }

        foreach (var member in team.EnumerateArray())
        {
            // Seuls cellId, champion, intention et position sont lus : jamais gameName, puuid, summonerId…
            var cell = Int(member, "cellId", -1);
            slots.Add(new ChampSelectSlot(
                cell,
                Int(member, "championId"),
                Int(member, "championPickIntent"),
                MapPosition(member.TryGetProperty("assignedPosition", out var pos) ? pos.GetString() : null),
                cell == localCell,
                locked.Contains(cell)));
        }

        return slots;
    }

    private sealed record Action(string Type, int ActorCellId, int ChampionId, bool Completed, bool IsAlly);

    private static IEnumerable<Action> Actions(JsonElement session)
    {
        if (!session.TryGetProperty("actions", out var groups) || groups.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var group in groups.EnumerateArray().Where(g => g.ValueKind == JsonValueKind.Array))
        {
            foreach (var a in group.EnumerateArray())
            {
                yield return new Action(
                    a.TryGetProperty("type", out var type) ? type.GetString() ?? string.Empty : string.Empty,
                    Int(a, "actorCellId", -1),
                    Int(a, "championId"),
                    a.TryGetProperty("completed", out var c) && c.GetBoolean(),
                    a.TryGetProperty("isAllyAction", out var ally) && ally.GetBoolean());
            }
        }
    }

    private static int Int(JsonElement e, string name, int fallback = 0) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : fallback;

    private static long Long(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var i) ? i : 0;

    private static double Double(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private static IEnumerable<int> Ints(JsonElement e, string name) =>
        e.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array
            ? arr.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt32()).Where(x => x > 0)
            : [];
}
