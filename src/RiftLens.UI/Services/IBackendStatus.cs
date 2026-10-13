namespace RiftLens.UI.Services;

public enum BackendState
{
    Unknown,
    Online,
    Unauthorized,
    Offline,
}

/// <summary>État de la connexion au backend RiftLens (fourni par l'hôte).</summary>
public interface IBackendStatus
{
    BackendState State { get; }

    /// <summary>Dernières infos du serveur (version, état de la clé Riot), null tant qu'il n'a pas répondu.</summary>
    RiftLens.Contracts.ServerInfoDto? ServerInfo { get; }

    event Action? Changed;

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
