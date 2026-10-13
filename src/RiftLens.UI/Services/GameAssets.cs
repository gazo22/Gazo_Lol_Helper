using RiftLens.Contracts;

namespace RiftLens.UI.Services;

/// <summary>Noms et URLs d'images Data Dragon, à partir du manifeste exposé par le backend.</summary>
public sealed class GameAssets(RiftLensApiClient api) : IDisposable
{
    private const string Cdn = "https://ddragon.leagueoflegends.com/cdn";
    private readonly SemaphoreSlim _lock = new(1, 1);
    private StaticManifestDto _manifest = StaticManifestDto.Empty;

    public bool IsLoaded => _manifest.Version.Length > 0;

    public string Version => _manifest.Version;

    public async Task EnsureLoadedAsync(CancellationToken ct = default)
    {
        if (IsLoaded)
        {
            return;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (!IsLoaded && await api.GetStaticManifestAsync(ct) is { IsSuccess: true } result)
            {
                _manifest = result.Value!;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();

    public string ChampionName(int id) => _manifest.Champions.TryGetValue(id, out var c) ? c.Name : $"#{id}";

    public string? ChampionKey(int id) => _manifest.Champions.TryGetValue(id, out var c) ? c.Key : null;

    public string? ChampionIcon(int id) => ChampionKey(id) is { } key && IsLoaded ? $"{Cdn}/{Version}/img/champion/{key}.png" : null;

    public string? ChampionSplash(int id) => ChampionKey(id) is { } key ? $"{Cdn}/img/champion/splash/{key}_0.jpg" : null;

    public string? ItemIcon(int id) => id > 0 && IsLoaded ? $"{Cdn}/{Version}/img/item/{id}.png" : null;

    public string ItemName(int id) => _manifest.Items.GetValueOrDefault(id, string.Empty);

    public string? ProfileIcon(int id) => IsLoaded ? $"{Cdn}/{Version}/img/profileicon/{id}.png" : null;

    public string? RuneIcon(int id) => _manifest.Runes.TryGetValue(id, out var r) ? $"{Cdn}/img/{r.Icon}" : null;

    public string RuneName(int id) => _manifest.Runes.TryGetValue(id, out var r) ? r.Name : string.Empty;

    public string? SpellIcon(int id) => _manifest.SummonerSpells.TryGetValue(id, out var s) && IsLoaded ? $"{Cdn}/{Version}/img/spell/{s.Key}.png" : null;

    public string SpellName(int id) => _manifest.SummonerSpells.TryGetValue(id, out var s) ? s.Name : string.Empty;
}
