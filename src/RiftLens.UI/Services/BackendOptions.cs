namespace RiftLens.UI.Services;

public sealed class BackendOptions
{
    public const string SectionName = "Backend";

    /// <summary>URL de base de l'Api RiftLens (ex. https://riftlens.mondomaine.fr/).</summary>
    public Uri BaseUrl { get; set; } = new("http://127.0.0.1:5080/");

    /// <summary>Clé d'accès partagée. Vide en installation locale : lue dans <c>%ProgramData%\RiftLens\access.key</c>. Serveur distant : user-secrets ou variable d'environnement.</summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>Source dynamique de la clé (fichier du service local), utilisée si <see cref="AccessKey"/> est vide.</summary>
    public Func<string?>? AccessKeyProvider { get; set; }
}
