using RiftLens.Contracts;
using RiftLens.Domain.Entities;

namespace RiftLens.Infrastructure.Matches;

/// <summary>Projection des entités vers les DTO exposés par l'Api.</summary>
public static class MatchDtoMapper
{
    public static MatchSummaryDto ToSummary(Match match, MatchParticipant me)
    {
        var teamKills = match.Participants.Where(p => p.TeamId == me.TeamId).Sum(p => p.Kills);
        return new MatchSummaryDto(
            match.MatchId, match.QueueId, match.GameStart, (int)match.Duration.TotalSeconds, match.IsRemake, me.Win,
            me.ChampionId, me.ChampLevel, me.Role.ToString(), me.Kills, me.Deaths, me.Assists, me.TotalCs,
            me.TotalDamageDealtToChampions, me.VisionScore, me.GoldEarned,
            teamKills == 0 ? 0 : (double)(me.Kills + me.Assists) / teamKills,
            me.Items, me.Summoners, me.Perks.ElementAtOrDefault(0), me.SubStyle,
            match.Participants.OrderBy(p => p.ParticipantId)
                .Select(p => new MatchPlayerLiteDto(p.TeamId, p.ChampionId, p.RiotIdGameName, p.RiotIdTagline)).ToList());
    }

    public static MatchDetailDto ToDetail(Match match)
    {
        var bansByTeam = match.Bans.ToLookup(b => b.TeamId);
        var teams = match.Participants
            .GroupBy(p => p.TeamId)
            .OrderBy(g => g.Key)
            .Select(g => new TeamDetailDto(
                g.Key,
                g.Any(p => p.Win),
                g.Sum(p => p.Kills),
                g.Sum(p => p.GoldEarned),
                bansByTeam[g.Key].OrderBy(b => b.PickTurn).Select(b => b.ChampionId).ToList(),
                g.OrderBy(p => p.ParticipantId).Select(ToPlayer).ToList()))
            .ToList();
        return new MatchDetailDto(match.MatchId, match.QueueId, match.GameVersion, match.GameStart, (int)match.Duration.TotalSeconds, match.IsRemake, teams);
    }

    private static MatchPlayerDto ToPlayer(MatchParticipant p) => new(
        p.ParticipantId, p.Puuid, p.RiotIdGameName, p.RiotIdTagline, p.ChampionId, p.ChampLevel, p.Role.ToString(), p.Win,
        p.Kills, p.Deaths, p.Assists, p.TotalCs, p.GoldEarned, p.TotalDamageDealtToChampions, p.DamageTaken, p.VisionScore,
        p.Items, p.Summoners, p.Perks, p.PrimaryStyle, p.SubStyle);
}
