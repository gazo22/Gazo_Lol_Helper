using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using RiftLens.Api.Endpoints;
using RiftLens.Api.Security;
using RiftLens.Contracts;
using RiftLens.Domain.ValueObjects;
using RiftLens.Infrastructure.Riot;
using RiftLens.Infrastructure.Riot.Dtos;

namespace RiftLens.Api.Tests;

public sealed class LocalModeTests(AccessKeyTests.ApiFactory factory) : IClassFixture<AccessKeyTests.ApiFactory>
{
    private const string GoodKey = "RGAPI-12345678-abcd-ef01-2345-6789abcdef01";

    [Fact]
    public async Task Hors_loopback_la_cle_Riot_ne_peut_pas_etre_modifiee()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiConventions.AccessKeyHeader, AccessKeyTests.ValidKey);

        // TestServer n'a pas d'adresse distante : traité comme non local.
        using var response = await client.PutAsJsonAsync(new Uri("/api/admin/riot-key", UriKind.Relative), new RiotKeyRequest(GoodKey));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Meta_expose_l_etat_de_la_cle()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiConventions.AccessKeyHeader, AccessKeyTests.ValidKey);
        var meta = await client.GetFromJsonAsync<ServerInfoDto>(new Uri("/api/meta", UriKind.Relative));
        Assert.Equal(RiotKeyStates.Unknown, meta!.RiotKey); // Riot:ApiKey fourni par la configuration de test
    }

    [Theory]
    [InlineData("pas-une-cle", StatusCodes.Status400BadRequest)]
    [InlineData("RGAPI-00000000-0000-0000-0000-000000000000", StatusCodes.Status422UnprocessableEntity)]
    [InlineData(GoodKey, StatusCodes.Status204NoContent)]
    public async Task Validation_puis_enregistrement_depuis_le_poste(string key, int expected)
    {
        var store = new Store();
        var http = new DefaultHttpContext { Connection = { RemoteIpAddress = IPAddress.Loopback } };

        var result = await MetaEndpoints.SetRiotKeyAsync(new RiotKeyRequest(key), http, store, new Riot(), Options.Create(new RiotApiOptions()), default);

        var status = result.Result switch
        {
            NoContent => StatusCodes.Status204NoContent,
            ProblemHttpResult p => p.StatusCode,
            _ => 0,
        };
        Assert.Equal(expected, status);
        Assert.Equal(expected == StatusCodes.Status204NoContent ? key : null, store.Saved);
    }

    [Fact]
    public void La_cle_d_acces_locale_est_generee_une_fois()
    {
        var dir = Path.Combine(Path.GetTempPath(), "riftlens-access-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = LocalAccessKey.EnsureCreated(dir);
            Assert.True(first.Length >= 32);
            Assert.Equal(first, LocalAccessKey.EnsureCreated(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class Store : IRiotKeyStore
    {
        public string? Saved { get; private set; }
        public RiotKeyState State => RiotKeyState.Missing;
        public event Action? KeyChanged;
        public string? GetKey() => Saved;
        public Task SetKeyAsync(string key, CancellationToken ct = default) { Saved = key; KeyChanged?.Invoke(); return Task.CompletedTask; }
        public void ReportAccepted() { }
        public void ReportRejected() { }
    }

    /// <summary>Riot accepte uniquement <see cref="GoodKey"/>.</summary>
    private sealed class Riot : IRiotApiClient
    {
        public Task<bool> ValidateKeyAsync(Platform platform, string key, CancellationToken ct = default) => Task.FromResult(key == GoodKey);
        public Task<AccountDto?> GetAccountByRiotIdAsync(RegionalRoute route, string gameName, string tagLine, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SummonerDto?> GetSummonerByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<LeagueEntryDto>> GetLeagueEntriesByPuuidAsync(Platform platform, string puuid, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<LeagueListDto?> GetApexLeagueAsync(Platform platform, RankTier tier, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ChampionMasteryDto>> GetTopMasteriesAsync(Platform platform, string puuid, int count, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetMatchIdsAsync(RegionalRoute route, string puuid, MatchIdQuery query, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<MatchDto?> GetMatchAsync(RegionalRoute route, string matchId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<TimelineDto?> GetTimelineAsync(RegionalRoute route, string matchId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
