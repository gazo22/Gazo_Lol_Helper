namespace RiftLens.Infrastructure.Local;

/// <summary>Mode « tout-en-un » : serveur installé comme service Windows sur le poste du joueur.</summary>
public sealed class LocalModeOptions
{
    public const string SectionName = "LocalMode";

    public bool Enabled { get; set; }

    /// <summary>Données du serveur local (clés, logs). Par défaut <c>%ProgramData%\RiftLens</c>.</summary>
    public string DataDirectory { get; set; } = DefaultDataDirectory;

    public static string DefaultDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RiftLens");

    public const string AccessKeyFileName = "access.key";
    public const string RiotKeyFileName = "riot.key";
}
