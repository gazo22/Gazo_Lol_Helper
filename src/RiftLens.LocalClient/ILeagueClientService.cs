using RiftLens.LocalClient.ChampSelect;
using RiftLens.LocalClient.Import;

namespace RiftLens.LocalClient;

public enum LeagueClientStatus
{
    /// <summary>Client LoL non détecté (fermé, ou lockfile introuvable).</summary>
    NotRunning,
    Connecting,
    Connected,
}

/// <summary>État du client LoL local et actions autorisées (import de runes / items uniquement, rien en jeu).</summary>
public interface ILeagueClientService
{
    LeagueClientStatus Status { get; }

    /// <summary>Champ select en cours, ou null.</summary>
    ChampSelectState? ChampSelect { get; }

    event Action? Changed;

    Task<ImportResult> ImportRunePageAsync(RunePageRequest request, CancellationToken ct = default);

    Task<ImportResult> ImportItemSetAsync(ItemSetRequest request, CancellationToken ct = default);
}
