using System.Net;
using System.Text;

namespace RiftLens.LocalClient.Tests;

internal static class Fixture
{
    public static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "lcu", name));
}

/// <summary>Faux client LoL : répond selon (méthode, chemin) et enregistre les requêtes avec leur corps.</summary>
internal sealed class FakeLcuHandler(Func<HttpMethod, string, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(HttpMethod Method, string Path, string? Body, string? Auth)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request.Method, request.RequestUri!.AbsolutePath, body, request.Headers.Authorization?.ToString()));
        return respond(request.Method, request.RequestUri.AbsolutePath);
    }

    public static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Status(HttpStatusCode code, string body = "") => new(code) { Content = new StringContent(body) };
}
