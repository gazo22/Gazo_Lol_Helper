namespace RiftLens.LocalClient.Connection;

public sealed class LocalClientOptions
{
    public const string SectionName = "LocalClient";

    /// <summary>Dossiers d'installation possibles de League of Legends (contenant le <c>lockfile</c>).</summary>
    public List<string> InstallPaths { get; } =
    [
        @"C:\Riot Games\League of Legends",
        @"D:\Riot Games\League of Legends",
    ];

    /// <summary>Intervalle de détection du client quand il n'est pas connecté.</summary>
    public TimeSpan DetectionInterval { get; set; } = TimeSpan.FromSeconds(3);
}
