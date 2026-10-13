using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiftLens.Contracts;

namespace RiftLens.Infrastructure.StaticData;

internal sealed partial class DataDragonService(
    HttpClient http,
    IOptions<DataDragonOptions> options,
    TimeProvider time,
    ILogger<DataDragonService> logger) : IStaticDataService, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private StaticManifestDto? _manifest;
    private DateTimeOffset _loadedAt;

    public async Task<StaticManifestDto> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        if (_manifest is not null && time.GetUtcNow() - _loadedAt < options.Value.RefreshInterval)
        {
            return _manifest;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_manifest is not null && time.GetUtcNow() - _loadedAt < options.Value.RefreshInterval)
            {
                return _manifest;
            }

            try
            {
                var versions = await http.GetFromJsonAsync<string[]>(new Uri("api/versions.json", UriKind.Relative), cancellationToken).ConfigureAwait(false);
                var version = versions?.FirstOrDefault() ?? throw new InvalidOperationException("versions.json vide.");
                if (_manifest?.Version != version)
                {
                    _manifest = await LoadAsync(version, cancellationToken).ConfigureAwait(false);
                    LogLoaded(logger, version, _manifest.Champions.Count);
                }

                _loadedAt = time.GetUtcNow();
            }
            catch (Exception ex) when (_manifest is not null && ex is HttpRequestException or JsonException or TaskCanceledException)
            {
                // Data Dragon indisponible : on garde la version précédente.
                LogStale(logger, ex, _manifest.Version);
                _loadedAt = time.GetUtcNow();
            }

            return _manifest;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<StaticManifestDto> LoadAsync(string version, CancellationToken ct)
    {
        var lang = options.Value.Language;
        var champions = LoadDocAsync($"cdn/{version}/data/{lang}/champion.json", ct);
        var items = LoadDocAsync($"cdn/{version}/data/{lang}/item.json", ct);
        var runes = LoadDocAsync($"cdn/{version}/data/{lang}/runesReforged.json", ct);
        var spells = LoadDocAsync($"cdn/{version}/data/{lang}/summoner.json", ct);
        await Task.WhenAll(champions, items, runes, spells).ConfigureAwait(false);

        using var c = await champions.ConfigureAwait(false);
        using var i = await items.ConfigureAwait(false);
        using var r = await runes.ConfigureAwait(false);
        using var s = await spells.ConfigureAwait(false);
        return Parse(version, c, i, r, s);
    }

    /// <summary>Transforme les fichiers Data Dragon bruts en manifeste compact (exposé pour les tests).</summary>
    internal static StaticManifestDto Parse(string version, JsonDocument champions, JsonDocument items, JsonDocument runes, JsonDocument spells)
    {
        var championMap = new Dictionary<int, ChampionStaticDto>();
        foreach (var champ in champions.RootElement.GetProperty("data").EnumerateObject())
        {
            var id = int.Parse(champ.Value.GetProperty("key").GetString()!, CultureInfo.InvariantCulture);
            championMap[id] = new ChampionStaticDto(champ.Value.GetProperty("id").GetString()!, champ.Value.GetProperty("name").GetString()!);
        }

        var itemMap = new Dictionary<int, string>();
        foreach (var item in items.RootElement.GetProperty("data").EnumerateObject())
        {
            if (int.TryParse(item.Name, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            {
                itemMap[id] = item.Value.GetProperty("name").GetString()!;
            }
        }

        var runeMap = new Dictionary<int, RuneStaticDto>();
        foreach (var tree in runes.RootElement.EnumerateArray())
        {
            AddRune(runeMap, tree);
            foreach (var slot in tree.GetProperty("slots").EnumerateArray())
            {
                foreach (var rune in slot.GetProperty("runes").EnumerateArray())
                {
                    AddRune(runeMap, rune);
                }
            }
        }

        var spellMap = new Dictionary<int, SummonerSpellStaticDto>();
        foreach (var spell in spells.RootElement.GetProperty("data").EnumerateObject())
        {
            var id = int.Parse(spell.Value.GetProperty("key").GetString()!, CultureInfo.InvariantCulture);
            spellMap[id] = new SummonerSpellStaticDto(spell.Value.GetProperty("id").GetString()!, spell.Value.GetProperty("name").GetString()!);
        }

        return new StaticManifestDto(version, championMap, itemMap, runeMap, spellMap);
    }

    private static void AddRune(Dictionary<int, RuneStaticDto> map, JsonElement rune) =>
        map[rune.GetProperty("id").GetInt32()] = new RuneStaticDto(rune.GetProperty("name").GetString()!, rune.GetProperty("icon").GetString()!);

    private async Task<JsonDocument> LoadDocAsync(string path, CancellationToken ct)
    {
        await using var stream = await http.GetStreamAsync(new Uri(path, UriKind.Relative), ct).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
    }

    public void Dispose() => _lock.Dispose();

    [LoggerMessage(Level = LogLevel.Information, Message = "Data Dragon {Version} chargé ({Champions} champions)")]
    private static partial void LogLoaded(ILogger logger, string version, int champions);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Data Dragon injoignable, conservation de la version {Version}")]
    private static partial void LogStale(ILogger logger, Exception ex, string version);
}
