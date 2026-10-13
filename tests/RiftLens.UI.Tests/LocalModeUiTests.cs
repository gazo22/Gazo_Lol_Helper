using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RiftLens.Contracts;
using RiftLens.UI.Layout;
using RiftLens.UI.Services;

namespace RiftLens.UI.Tests;

public sealed class LocalModeUiTests : BunitContext, IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "riftlens-cache-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("https://ddragon.leagueoflegends.com/cdn/15.20.1/img/champion/Ahri.png", true)]
    [InlineData("https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Ahri_0.jpg", true)]
    [InlineData("https://ddragon.leagueoflegends.com/api/versions.json", false)]
    [InlineData("http://ddragon.leagueoflegends.com/cdn/15.20.1/img/item/3157.png", false)]
    [InlineData("https://evil.example.com/cdn/img/x.png", false)]
    public void Seules_les_images_Data_Dragon_sont_cachees(string url, bool expected) =>
        Assert.Equal(expected, DiskAssetCache.IsCacheable(new Uri(url)));

    [Fact]
    public async Task Une_image_n_est_telechargee_qu_une_fois()
    {
        var calls = 0;
        var http = new HttpClient(new Handler(_ => { calls++; return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) }; }));
        var cache = new DiskAssetCache(http, _dir);
        var uri = new Uri("https://ddragon.leagueoflegends.com/cdn/15.20.1/img/champion/Ahri.png");

        var first = await cache.GetAsync(uri);
        var second = await cache.GetAsync(uri);

        Assert.Equal(first, second);
        Assert.Equal(1, calls);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(first!));
        Assert.Equal("image/png", DiskAssetCache.ContentType(first!));
    }

    [Fact]
    public async Task Une_erreur_CDN_n_ecrit_rien()
    {
        var cache = new DiskAssetCache(new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.NotFound))), _dir);
        Assert.Null(await cache.GetAsync(new Uri("https://ddragon.leagueoflegends.com/cdn/15.20.1/img/item/1.png")));
        Assert.False(Directory.Exists(_dir) && Directory.EnumerateFiles(_dir, "*", SearchOption.AllDirectories).Any());
    }

    [Theory]
    [InlineData(BackendState.Online, RiotKeyStates.Missing, "Aucune clé API Riot")]
    [InlineData(BackendState.Online, RiotKeyStates.Rejected, "a expiré")]
    [InlineData(BackendState.Offline, null, "injoignable")]
    public void Bandeau_global_selon_l_etat(BackendState state, string? key, string expected)
    {
        Services.AddSingleton<IBackendStatus>(new FakeStatus(state, key is null ? null : new ServerInfoDto("1.0.0", "Euw1", null, key, true)));
        var cut = Render<ServerBanner>();
        Assert.Contains(expected, cut.Find(".rl-banner").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Pas_de_bandeau_quand_tout_va_bien()
    {
        Services.AddSingleton<IBackendStatus>(new FakeStatus(BackendState.Online, new ServerInfoDto("1.0.0", "Euw1", null, RiotKeyStates.Valid, true)));
        Assert.Empty(Render<ServerBanner>().FindAll(".rl-banner"));
    }

    [Fact]
    public async Task La_cle_d_acces_locale_est_relue_tant_qu_elle_manque()
    {
        string? fileKey = null;
        string? sent = null;
        var handler = new AccessKeyHandler(new BackendOptions { AccessKeyProvider = () => fileKey })
        {
            InnerHandler = new Handler(r => { sent = r.Headers.TryGetValues(ApiConventions.AccessKeyHeader, out var v) ? v.Single() : null; return new HttpResponseMessage(HttpStatusCode.OK); }),
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5080/") };

        await client.GetAsync(new Uri("api/meta", UriKind.Relative));
        Assert.Null(sent);

        fileKey = "cle-generee-par-le-service";
        await client.GetAsync(new Uri("api/meta", UriKind.Relative));
        Assert.Equal("cle-generee-par-le-service", sent);
    }

    private sealed class FakeStatus(BackendState state, ServerInfoDto? info) : IBackendStatus
    {
        public BackendState State => state;
        public ServerInfoDto? ServerInfo => info;
#pragma warning disable CS0067
        public event Action? Changed;
#pragma warning restore CS0067
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }

    void IDisposable.Dispose()
    {
        if (Directory.Exists(_dir)) { Directory.Delete(_dir, recursive: true); }
        Dispose();
    }
}
