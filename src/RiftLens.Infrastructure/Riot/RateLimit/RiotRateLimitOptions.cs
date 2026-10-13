namespace RiftLens.Infrastructure.Riot.RateLimit;

public enum RequestPriority
{
    Background = 0,
    Interactive = 1,
}

public sealed class RiotRateLimitOptions
{
    public const string SectionName = "Riot:RateLimit";

    /// <summary>Limites applicatives supposées avant le premier en-tête reçu (clé perso : 20/1 s, 100/2 min).</summary>
    public string DefaultAppLimits { get; set; } = "20:1,100:120";

    /// <summary>
    /// Part du budget consommable par ce processus (0-1]. L'Api et le Collector partagent la même clé
    /// dans deux processus : ex. 0.4 pour l'Api, 0.6 pour le Collector.
    /// </summary>
    public double BudgetShare { get; set; } = 1.0;

    /// <summary>Priorité par défaut des requêtes de ce processus.</summary>
    public RequestPriority DefaultPriority { get; set; } = RequestPriority.Interactive;

    /// <summary>Nombre maximal de nouvelles tentatives après un 429.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Attente appliquée sur un 429 sans en-tête Retry-After (limite de service).</summary>
    public TimeSpan DefaultRetryAfter { get; set; } = TimeSpan.FromSeconds(2);
}
