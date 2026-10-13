using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RiftLens.Contracts;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Players;
using RiftLens.Infrastructure.Riot;

namespace RiftLens.Api.Tests;

public sealed class PlayerEndpointsTests(PlayerEndpointsTests.Factory factory) : IClassFixture<PlayerEndpointsTests.Factory>
{
    public sealed class Factory : AccessKeyTests.ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(s => s.AddScoped<IPlayerService, FakePlayerService>());
        }
    }

    private sealed class FakePlayerService : IPlayerService
    {
        public Task<ProfileDto> GetProfileAsync(Platform platform, RiotId riotId, bool forceRefresh, CancellationToken ct = default) => riotId.GameName switch
        {
            "Inconnu" => throw new PlayerNotFoundException("Joueur introuvable."),
            "CleExpiree" => throw new RiotApiAuthException(HttpStatusCode.Forbidden, "Forbidden"),
            _ => Task.FromResult(new ProfileDto("p", riotId.GameName, riotId.TagLine, platform.ToString(), 1, 30, null, null, [], ProfileStatsDto.Empty, [], [], [], DateTimeOffset.UnixEpoch)),
        };
    }

    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiConventions.AccessKeyHeader, AccessKeyTests.ValidKey);
        return client;
    }

    [Fact]
    public async Task Profil_existant_renvoie_200()
    {
        using var client = Client();
        var profile = await client.GetFromJsonAsync<ProfileDto>(new Uri("/api/players/euw1/Le%20Gazo/EUW", UriKind.Relative));
        Assert.Equal(("Le Gazo", "Euw1"), (profile!.GameName, profile.Platform));
    }

    [Theory]
    [InlineData("/api/players/euw1/Inconnu/EUW", HttpStatusCode.NotFound, "player-not-found")]
    [InlineData("/api/players/euw1/CleExpiree/EUW", HttpStatusCode.ServiceUnavailable, "riot-key-invalid")]
    public async Task Les_erreurs_sont_traduites_en_problem_details(string url, HttpStatusCode status, string code)
    {
        using var client = Client();
        using var response = await client.GetAsync(new Uri(url, UriKind.Relative));
        Assert.Equal(status, response.StatusCode);
        Assert.Contains(code, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/api/players/mars1/Gazo/EUW")]
    [InlineData("/api/players/euw1/Gazo/X")]
    public async Task Les_parametres_invalides_renvoient_400(string url)
    {
        using var client = Client();
        using var response = await client.GetAsync(new Uri(url, UriKind.Relative));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
