using RiftLens.Contracts;
using RiftLens.Infrastructure.Riot.Dtos;

namespace RiftLens.Infrastructure.Matches;

public static class GoldDiffCalculator
{
    public const int BlueTeam = 100;

    /// <summary>Or total de chaque équipe à chaque frame (une par minute) de la timeline.</summary>
    public static IReadOnlyList<GoldDiffPointDto> Compute(TimelineDto timeline, IReadOnlyDictionary<int, int> teamByParticipant)
    {
        var points = new List<GoldDiffPointDto>(timeline.Info.Frames.Count);
        var interval = timeline.Info.FrameInterval > 0 ? timeline.Info.FrameInterval : 60_000;
        foreach (var frame in timeline.Info.Frames)
        {
            int blue = 0, red = 0;
            foreach (var pf in frame.ParticipantFrames.Values)
            {
                var team = teamByParticipant.TryGetValue(pf.ParticipantId, out var t) ? t : (pf.ParticipantId <= 5 ? BlueTeam : 200);
                if (team == BlueTeam) { blue += pf.TotalGold; } else { red += pf.TotalGold; }
            }

            points.Add(new GoldDiffPointDto((int)Math.Round(frame.Timestamp / (double)interval), blue, red));
        }

        return points;
    }
}
