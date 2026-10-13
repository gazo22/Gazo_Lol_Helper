using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;
using RiftLens.LocalClient.Connection;
using RiftLens.LocalClient.Import;

namespace RiftLens.LocalClient.Tests;

public class ImportTests
{
    private static readonly RunePageRequest Runes = new("RiftLens Ahri (Middle)", 8100, 8200, [8112, 8139, 8138, 8135, 8226, 8210, 5005, 5008, 5011]);

    private static (LcuHttpClient Client, FakeLcuHandler Handler) Create(Func<HttpMethod, string, HttpResponseMessage> respond)
    {
        var handler = new FakeLcuHandler(respond);
        return (new LcuHttpClient(new LcuCredentials(52437, "pw"), handler), handler);
    }

    [Fact]
    public async Task Import_runes_remplace_l_ancienne_page_RiftLens_et_preserve_les_autres()
    {
        var (lcu, handler) = Create((method, path) => method == HttpMethod.Get ? FakeLcuHandler.Json(Fixture.Read("perk-pages.json")) : FakeLcuHandler.Status(HttpStatusCode.OK, "{}"));
        using (lcu)
        {
            var result = await RunePageImporter.ImportAsync(lcu, Runes);

            Assert.True(result.Success);
            var delete = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Delete);
            Assert.Equal("/lol-perks/v1/pages/1874560322", delete.Path); // seulement « RiftLens Ahri »

            var post = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Post);
            var body = JsonNode.Parse(post.Body!)!;
            Assert.Equal("RiftLens Ahri (Middle)", (string)body["name"]!);
            Assert.Equal(8100, (int)body["primaryStyleId"]!);
            Assert.Equal(9, body["selectedPerkIds"]!.AsArray().Count);
            Assert.True((bool)body["current"]!);
            Assert.All(handler.Requests, r => Assert.Equal("Basic cmlvdDpwdw==", r.Auth));
        }
    }

    [Fact]
    public async Task Sans_emplacement_libre_on_explique_sans_supprimer_les_pages_du_joueur()
    {
        var (lcu, handler) = Create((method, _) => method == HttpMethod.Get ? FakeLcuHandler.Json("[]")
            : FakeLcuHandler.Status(HttpStatusCode.BadRequest, "{\"message\":\"Max pages reached\"}"));
        using (lcu)
        {
            var result = await RunePageImporter.ImportAsync(lcu, Runes);

            Assert.False(result.Success);
            Assert.Contains("emplacement", result.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(handler.Requests, r => r.Method == HttpMethod.Delete);
        }
    }

    [Fact]
    public async Task Page_incomplete_refusee_sans_appel() =>
        Assert.False((await RunePageImporter.ImportAsync(null!, Runes with { Perks = [8112] })).Success);

    [Fact]
    public async Task Import_item_set_conserve_les_sets_du_joueur()
    {
        var (lcu, handler) = Create((method, path) => (method.Method, path) switch
        {
            ("GET", "/lol-summoner/v1/current-summoner") => FakeLcuHandler.Json(Fixture.Read("current-summoner.json")),
            ("GET", _) => FakeLcuHandler.Json(Fixture.Read("item-sets.json")),
            _ => FakeLcuHandler.Status(HttpStatusCode.NoContent),
        });
        using (lcu)
        {
            var request = new ItemSetRequest("RiftLens · Ahri", 103, [new ItemSetBlock("Core", [3157, 4645, 3089])]);
            var result = await ItemSetImporter.ImportAsync(lcu, request, new FakeTimeProvider(DateTimeOffset.FromUnixTimeMilliseconds(1760340000000)));

            Assert.True(result.Success);
            var put = Assert.Single(handler.Requests, r => r.Method == HttpMethod.Put);
            Assert.Equal("/lol-item-sets/v1/item-sets/98765432/sets", put.Path);

            var sets = JsonNode.Parse(put.Body!)!["itemSets"]!.AsArray();
            Assert.Equal(2, sets.Count);
            Assert.Contains(sets, s => (string)s!["title"]! == "Mon set Ahri"); // set du joueur intact
            var ours = Assert.Single(sets, s => (string)s!["uid"]! == "riftlens-103");
            Assert.Equal("RiftLens · Ahri", (string)ours!["title"]!);
            Assert.Equal(["3157", "4645", "3089"], ours["blocks"]![0]!["items"]!.AsArray().Select(i => (string)i!["id"]!));
            Assert.Equal(1760340000000, (long)JsonNode.Parse(put.Body!)!["timestamp"]!);
        }
    }
}
