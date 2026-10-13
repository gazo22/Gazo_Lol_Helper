using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RiftLens.LocalClient;
using RiftLens.LocalClient.ChampSelect;
using RiftLens.LocalClient.Import;
using RiftLens.UI.Pages;
using RiftLens.UI.Services;

namespace RiftLens.UI.Tests;

public sealed class DraftPageTests : BunitContext
{
    private readonly FakeClient _client = new();

    public DraftPageTests()
    {
        var http = new HttpClient(new NotFound()) { BaseAddress = new Uri("http://localhost/") };
        Services.AddSingleton(new RiftLensApiClient(http));
        Services.AddSingleton<GameAssets>();
        Services.AddSingleton<ILeagueClientService>(_client);
    }

    [Fact]
    public void Client_absent()
    {
        var cut = Render<Draft>();
        Assert.Contains("Client League of Legends non détecté", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Champ_select_affiche_postes_sans_noms_et_se_met_a_jour()
    {
        var cut = Render<Draft>();

        _client.Set(LeagueClientStatus.Connected, new ChampSelectState(1, 2, "BAN_PICK", TimeSpan.FromSeconds(25),
            [new(0, 266, 0, "Top", false, true), new(1, 0, 0, "Jungle", false, false), new(2, 0, 103, "Middle", true, false), new(3, 0, 0, "Bottom", false, false), new(4, 0, 0, "Support", false, false)],
            [new(5, 122, 0, null, false, true), new(6, 0, 0, null, false, false), new(7, 0, 0, null, false, false), new(8, 0, 0, null, false, false), new(9, 0, 0, null, false, false)],
            [157], [238]));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(5, cut.FindAll(".rl-draft__team--blue .rl-draft-slot").Count);
            Assert.Single(cut.FindAll(".rl-draft-slot.is-local"));
            Assert.Contains("Vous", cut.Find(".rl-draft-slot.is-local").TextContent, StringComparison.Ordinal);
            Assert.Equal("Champ select · bans & picks · 25 s", cut.Find(".rl-page__header .rl-status").TextContent.Trim());
        });

        _client.Set(LeagueClientStatus.Connected, null);
        cut.WaitForAssertion(() => Assert.Contains("En attente du champ select", cut.Markup, StringComparison.Ordinal));
    }

    private sealed class FakeClient : ILeagueClientService
    {
        public LeagueClientStatus Status { get; private set; }
        public ChampSelectState? ChampSelect { get; private set; }
        public event Action? Changed;

        public void Set(LeagueClientStatus status, ChampSelectState? state)
        {
            (Status, ChampSelect) = (status, state);
            Changed?.Invoke();
        }

        public Task<ImportResult> ImportRunePageAsync(RunePageRequest request, CancellationToken ct = default) => Task.FromResult(ImportResult.Ok("ok"));
        public Task<ImportResult> ImportItemSetAsync(ItemSetRequest request, CancellationToken ct = default) => Task.FromResult(ImportResult.Ok("ok"));
    }

    private sealed class NotFound : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
