namespace RiftLens.UI.Services;

public sealed class BackendOptions
{
    public const string SectionName = "Backend";

    /// <summary>URL de base de l'Api RiftLens (ex. https://riftlens.mondomaine.fr/).</summary>
    public Uri BaseUrl { get; set; } = new("http://localhost:5080/");

    /// <summary>Clé d'accès partagée. Ne pas la mettre dans appsettings.json : user-secrets ou variable d'environnement.</summary>
    public string AccessKey { get; set; } = string.Empty;
}
