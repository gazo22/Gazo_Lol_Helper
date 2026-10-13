using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Riot.Dtos;

namespace RiftLens.Infrastructure.Matches;

/// <summary>Convertit un <see cref="MatchDto"/> Match-V5 en entités persistées.</summary>
public static class MatchMapper
{
    /// <summary>En dessous de cette durée, une partie terminée par reddition est un remake.</summary>
    private static readonly TimeSpan RemakeThreshold = TimeSpan.FromMinutes(5);

    public static Match ToEntity(MatchDto dto)
    {
        var info = dto.Info;
        var patch = Patch.TryParse(info.GameVersion, out var parsed) ? parsed : null;

        // Depuis le patch 11.20, gameDuration est en secondes (auparavant en millisecondes).
        var duration = info.GameEndTimestamp is null ? TimeSpan.FromMilliseconds(info.GameDuration) : TimeSpan.FromSeconds(info.GameDuration);
        var earlySurrender = info.Participants.Any(p => p.GameEndedInEarlySurrender);

        var match = new Match
        {
            MatchId = dto.Metadata.MatchId,
            QueueId = info.QueueId,
            GameVersion = info.GameVersion,
            PatchMajor = patch?.Major ?? 0,
            PatchMinor = patch?.Minor ?? 0,
            GameStart = DateTimeOffset.FromUnixTimeMilliseconds(info.GameStartTimestamp),
            Duration = duration,
            IsRemake = earlySurrender && duration < RemakeThreshold,
        };

        foreach (var p in info.Participants)
        {
            match.Participants.Add(ToEntity(match.MatchId, p));
        }

        foreach (var team in info.Teams)
        {
            foreach (var ban in team.Bans.Where(b => b.ChampionId > 0))
            {
                match.Bans.Add(new MatchBan { MatchId = match.MatchId, TeamId = team.TeamId, PickTurn = ban.PickTurn, ChampionId = ban.ChampionId });
            }
        }

        return match;
    }

    private static MatchParticipant ToEntity(string matchId, ParticipantDto p)
    {
        var styles = p.Perks?.Styles ?? [];
        var primary = styles.FirstOrDefault(s => s.Description == "primaryStyle") ?? styles.ElementAtOrDefault(0);
        var sub = styles.FirstOrDefault(s => s.Description == "subStyle") ?? styles.ElementAtOrDefault(1);
        var perks = new int[9];
        var selections = (primary?.Selections ?? []).Concat(sub?.Selections ?? []).Select(s => s.Perk).Take(6).ToArray();
        selections.CopyTo(perks, 0);
        if (p.Perks?.StatPerks is { } stats)
        {
            perks[6] = stats.Offense;
            perks[7] = stats.Flex;
            perks[8] = stats.Defense;
        }

        return new MatchParticipant
        {
            MatchId = matchId,
            ParticipantId = p.ParticipantId,
            Puuid = p.Puuid,
            RiotIdGameName = p.RiotIdGameName,
            RiotIdTagline = p.RiotIdTagline,
            TeamId = p.TeamId,
            Win = p.Win,
            ChampionId = p.ChampionId,
            Role = RoleExtensions.FromTeamPosition(p.TeamPosition),
            ChampLevel = p.ChampLevel,
            Kills = p.Kills,
            Deaths = p.Deaths,
            Assists = p.Assists,
            TotalMinionsKilled = p.TotalMinionsKilled,
            NeutralMinionsKilled = p.NeutralMinionsKilled,
            GoldEarned = p.GoldEarned,
            TotalDamageDealtToChampions = p.TotalDamageDealtToChampions,
            DamageTaken = p.TotalDamageTaken,
            VisionScore = p.VisionScore,
            WardsPlaced = p.WardsPlaced,
            Items = [p.Item0, p.Item1, p.Item2, p.Item3, p.Item4, p.Item5, p.Item6],
            Summoners = [p.Summoner1Id, p.Summoner2Id],
            PrimaryStyle = primary?.Style ?? 0,
            SubStyle = sub?.Style ?? 0,
            Perks = perks,
        };
    }
}
