using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RiftLens.Contracts;

namespace RiftLens.UI.Services;

/// <summary>Client typé du backend RiftLens.</summary>
public sealed class RiftLensApiClient(HttpClient http)
{
    public Task<ApiResult<ProfileDto>> GetProfileAsync(string platform, string gameName, string tagLine, bool refresh = false, CancellationToken ct = default) =>
        GetAsync<ProfileDto>($"api/players/{Esc(platform)}/{Esc(gameName)}/{Esc(tagLine)}{(refresh ? "?refresh=true" : string.Empty)}", ct);

    public Task<ApiResult<List<MatchSummaryDto>>> GetHistoryAsync(string platform, string puuid, int start, int count, CancellationToken ct = default) =>
        GetAsync<List<MatchSummaryDto>>(FormattableString.Invariant($"api/players/{Esc(platform)}/by-puuid/{Esc(puuid)}/matches?start={start}&count={count}"), ct);

    public Task<ApiResult<MatchDetailDto>> GetMatchAsync(string matchId, CancellationToken ct = default) =>
        GetAsync<MatchDetailDto>($"api/matches/{Esc(matchId)}", ct);

    public Task<ApiResult<List<GoldDiffPointDto>>> GetGoldDiffAsync(string matchId, CancellationToken ct = default) =>
        GetAsync<List<GoldDiffPointDto>>($"api/matches/{Esc(matchId)}/gold-diff", ct);

    public Task<ApiResult<StaticManifestDto>> GetStaticManifestAsync(CancellationToken ct = default) =>
        GetAsync<StaticManifestDto>("api/static/manifest", ct);

    public Task<ApiResult<PatchListDto>> GetStatPatchesAsync(CancellationToken ct = default) =>
        GetAsync<PatchListDto>("api/stats/patches", ct);

    public Task<ApiResult<TierListDto>> GetTierListAsync(string? patch, string? role, string elo, CancellationToken ct = default) =>
        GetAsync<TierListDto>($"api/tierlist?elo={Esc(elo)}{Opt("patch", patch)}{Opt("role", role)}", ct);

    public Task<ApiResult<ChampionDetailDto>> GetChampionAsync(int championId, string? patch, string? role, string elo, CancellationToken ct = default) =>
        GetAsync<ChampionDetailDto>(FormattableString.Invariant($"api/champions/{championId}?elo={Esc(elo)}") + Opt("patch", patch) + Opt("role", role), ct);

    public Task<ApiResult<MatchupListDto>> GetMatchupsAsync(int championId, string? role, string elo, CancellationToken ct = default) =>
        GetAsync<MatchupListDto>(FormattableString.Invariant($"api/champions/{championId}/matchups?elo={Esc(elo)}") + Opt("role", role), ct);

    /// <summary>Enregistre la clé Riot sur le serveur local. Renvoie null si OK, sinon le message d'erreur.</summary>
    public async Task<string?> SetRiotKeyAsync(string key, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PutAsJsonAsync(new Uri("api/admin/riot-key", UriKind.Relative), new RiotKeyRequest(key), ct);
            if (response.IsSuccessStatusCode)
            {
                return null;
            }

            var problem = await ReadProblemAsync(response, ct);
            return problem.Title ?? $"Erreur {(int)response.StatusCode}.";
        }
        catch (HttpRequestException)
        {
            return "Serveur RiftLens injoignable.";
        }
    }

    private static string Opt(string name, string? value) => string.IsNullOrEmpty(value) ? string.Empty : $"&{name}={Esc(value)}";

    private async Task<ApiResult<T>> GetAsync<T>(string relative, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(new Uri(relative, UriKind.Relative), ct);
            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadFromJsonAsync<T>(ct);
                return value is null ? ApiResult.Fail<T>(ApiError.Server) : ApiResult.Ok(value);
            }

            var problem = await ReadProblemAsync(response, ct);
            return response.StatusCode switch
            {
                HttpStatusCode.NotFound => ApiResult.Fail<T>(ApiError.NotFound, problem.Title),
                HttpStatusCode.Unauthorized => ApiResult.Fail<T>(ApiError.Unauthorized, problem.Title),
                HttpStatusCode.TooManyRequests => ApiResult.Fail<T>(ApiError.RateLimited, problem.Title),
                HttpStatusCode.ServiceUnavailable when problem.Code == "riot-key-invalid" => ApiResult.Fail<T>(ApiError.RiotKeyInvalid, problem.Title),
                HttpStatusCode.ServiceUnavailable when problem.Code == "riot-key-missing" => ApiResult.Fail<T>(ApiError.RiotKeyMissing, problem.Title),
                _ => ApiResult.Fail<T>(ApiError.Server, problem.Title),
            };
        }
        catch (HttpRequestException ex)
        {
            return ApiResult.Fail<T>(ApiError.Offline, ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult.Fail<T>(ApiError.Offline, "Délai dépassé.");
        }
    }

    private static async Task<(string? Title, string? Code)> ReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var root = doc.RootElement;
            return (root.TryGetProperty("title", out var t) ? t.GetString() : null,
                    root.TryGetProperty("code", out var c) ? c.GetString() : null);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string Esc(string value) => Uri.EscapeDataString(value);
}
