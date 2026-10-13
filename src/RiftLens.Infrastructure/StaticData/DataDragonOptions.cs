namespace RiftLens.Infrastructure.StaticData;

public sealed class DataDragonOptions
{
    public const string SectionName = "DataDragon";

    public Uri BaseUrl { get; set; } = new("https://ddragon.leagueoflegends.com/");

    public string Language { get; set; } = "fr_FR";

    /// <summary>Fréquence de vérification d'une nouvelle version.</summary>
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(6);
}
