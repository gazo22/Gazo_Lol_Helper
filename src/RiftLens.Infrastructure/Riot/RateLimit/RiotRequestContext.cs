namespace RiftLens.Infrastructure.Riot.RateLimit;

/// <summary>
/// Priorité ambiante des appels Riot du flux d'exécution courant. Le Collector s'exécute en <see cref="RequestPriority.Background"/>
/// même lorsqu'il partage le processus de l'Api, dont les requêtes restent interactives.
/// </summary>
public static class RiotRequestContext
{
    private static readonly AsyncLocal<RequestPriority?> Current = new();

    public static RequestPriority? Priority => Current.Value;

    public static IDisposable WithPriority(RequestPriority priority)
    {
        var previous = Current.Value;
        Current.Value = priority;
        return new Restore(previous);
    }

    private sealed class Restore(RequestPriority? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
