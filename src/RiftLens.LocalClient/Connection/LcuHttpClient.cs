using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RiftLens.LocalClient.Connection;

/// <summary>Appels REST vers le client LoL local (HTTPS 127.0.0.1, auth Basic <c>riot:&lt;password&gt;</c>).</summary>
public sealed class LcuHttpClient : IDisposable
{
    private readonly HttpClient _http;

    public LcuHttpClient(LcuCredentials credentials, HttpMessageHandler? handler = null)
    {
        Credentials = credentials;
        _http = new HttpClient(handler ?? LoopbackCertificatePolicy.CreateHandler(), disposeHandler: true)
        {
            BaseAddress = credentials.BaseUri,
            Timeout = TimeSpan.FromSeconds(10),
        };
        _http.DefaultRequestHeaders.Authorization = credentials.AuthorizationHeader;
    }

    public LcuCredentials Credentials { get; }

    /// <summary>Renvoie null sur 404 (ex. pas de champ select en cours).</summary>
    public async Task<JsonNode?> GetAsync(string path, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(Relative(path), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, ct);
        return await JsonNode.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    }

    public async Task<JsonElement?> GetElementAsync(string path, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(Relative(path), ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, ct);
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.Clone();
    }

    public async Task PostAsync(string path, JsonNode body, CancellationToken ct = default)
    {
        using var response = await _http.PostAsJsonAsync(Relative(path), body, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task PutAsync(string path, JsonNode body, CancellationToken ct = default)
    {
        using var response = await _http.PutAsJsonAsync(Relative(path), body, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync(Relative(path), ct);
        await EnsureSuccessAsync(response, ct);
    }

    private static Uri Relative(string path) => new(path.TrimStart('/'), UriKind.Relative);

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new LcuRequestException(response.StatusCode, body);
        }
    }

    public void Dispose() => _http.Dispose();
}

public sealed class LcuRequestException : Exception
{
    public LcuRequestException() { }

    public LcuRequestException(string message) : base(message) { }

    public LcuRequestException(string message, Exception inner) : base(message, inner) { }

    public LcuRequestException(HttpStatusCode status, string body) : base($"LCU {(int)status} : {body}")
    {
        StatusCode = status;
        Body = body;
    }

    public HttpStatusCode? StatusCode { get; }

    public string? Body { get; }
}
