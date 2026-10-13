using RiftLens.Contracts;
using RiftLens.Domain.ValueObjects;

namespace RiftLens.Infrastructure.Players;

public interface IPlayerService
{
    /// <exception cref="PlayerNotFoundException">Riot ID inconnu.</exception>
    Task<ProfileDto> GetProfileAsync(Platform platform, RiotId riotId, bool forceRefresh, CancellationToken ct = default);
}
