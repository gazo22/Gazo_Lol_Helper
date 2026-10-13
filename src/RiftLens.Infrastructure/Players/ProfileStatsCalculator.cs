using RiftLens.Contracts;
using RiftLens.Domain.Entities;
using RiftLens.Domain.ValueObjects;

namespace RiftLens.Infrastructure.Players;

/// <summary>Statistiques agrégées d'un joueur à partir de ses parties stockées.</summary>
public static class ProfileStatsCalculator
{
    /// <summary>Files jouées sur la Faille de l'invocateur prises en compte (normales, classées, partie rapide).</summary>
    public static readonly IReadOnlySet<int> RiftQueues = new HashSet<int> { 400, 420, 430, 440, 490 };

    public sealed record Sample(Match Match, MatchParticipant Me);

    public static ProfileStatsDto Compute(IReadOnlyCollection<Sample> samples)
    {
        var valid = samples.Where(s => !s.Match.IsRemake && s.Match.Duration > TimeSpan.Zero).ToList();
        if (valid.Count == 0)
        {
            return ProfileStatsDto.Empty;
        }

        var minutes = valid.Sum(s => s.Match.Duration.TotalMinutes);
        var killParticipation = valid.Average(s =>
        {
            var teamKills = s.Match.Participants.Where(p => p.TeamId == s.Me.TeamId).Sum(p => p.Kills);
            return teamKills == 0 ? 0 : (double)(s.Me.Kills + s.Me.Assists) / teamKills;
        });

        return new ProfileStatsDto(
            valid.Count,
            valid.Count(s => s.Me.Win),
            valid.Average(s => s.Me.Kills),
            valid.Average(s => s.Me.Deaths),
            valid.Average(s => s.Me.Assists),
            valid.Sum(s => s.Me.TotalCs) / minutes,
            valid.Sum(s => s.Me.VisionScore) / minutes,
            valid.Sum(s => s.Me.TotalDamageDealtToChampions) / minutes,
            killParticipation);
    }

    public static IReadOnlyList<ChampionPerformanceDto> TopChampions(IReadOnlyCollection<Sample> samples, int take = 5) =>
        samples.Where(s => !s.Match.IsRemake)
            .GroupBy(s => s.Me.ChampionId)
            .Select(g =>
            {
                var deaths = g.Sum(s => s.Me.Deaths);
                var ka = g.Sum(s => s.Me.Kills + s.Me.Assists);
                var minutes = g.Sum(s => s.Match.Duration.TotalMinutes);
                return new ChampionPerformanceDto(
                    g.Key, g.Count(), g.Count(s => s.Me.Win),
                    deaths == 0 ? ka : (double)ka / deaths,
                    minutes == 0 ? 0 : g.Sum(s => s.Me.TotalCs) / minutes);
            })
            .OrderByDescending(c => c.Games).ThenByDescending(c => c.Winrate)
            .Take(take)
            .ToList();

    public static IReadOnlyList<RoleShareDto> Roles(IReadOnlyCollection<Sample> samples) =>
        samples.Where(s => !s.Match.IsRemake && s.Me.Role != Role.Unknown)
            .GroupBy(s => s.Me.Role)
            .Select(g => new RoleShareDto(g.Key.ToString(), g.Count(), g.Count(s => s.Me.Win)))
            .OrderByDescending(r => r.Games)
            .ToList();
}
