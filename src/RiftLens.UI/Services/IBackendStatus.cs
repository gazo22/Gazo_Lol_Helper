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

    event Action? Changed;

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
