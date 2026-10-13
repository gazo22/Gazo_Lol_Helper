using RiftLens.Contracts;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Riot.RateLimit;

namespace RiftLens.Infrastructure.Matches;

public interface IMatchService
{
    /// <summary>Télécharge les matchs récents absents de la base. Renvoie le nombre de matchs nouvellement stockés.</summary>
    Task<int> SyncAsync(Platform platform, string puuid, int start, int count, RequestPriority? priority = null, CancellationToken ct = default);

    Task<IReadOnlyList<MatchSummaryDto>> GetHistoryAsync(Platform platform, string puuid, int start, int count, CancellationToken ct = default);

    Task<MatchDetailDto?> GetDetailAsync(string matchId, CancellationToken ct = default);

    Task<IReadOnlyList<GoldDiffPointDto>?> GetGoldDiffAsync(string matchId, CancellationToken ct = default);
}
