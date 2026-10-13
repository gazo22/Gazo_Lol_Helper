using System.Globalization;
using System.Text.Json.Nodes;
using RiftLens.LocalClient.Connection;

namespace RiftLens.LocalClient.Import;

/// <summary>
/// Écrit un item set pour un champion. Les sets du joueur sont conservés tels quels ;
/// seul le précédent set RiftLens du même champion est remplacé.
/// </summary>
public static class ItemSetImporter
{
    public const string UidPrefix = "riftlens-";
    private const int SummonersRift = 11;

    public static async Task<ImportResult> ImportAsync(LcuHttpClient lcu, ItemSetRequest request, TimeProvider time, CancellationToken ct = default)
    {
        var summoner = await lcu.GetAsync("lol-summoner/v1/current-summoner", ct)
            ?? throw new LcuRequestException("Aucun invocateur connecté dans le client.");
        var summonerId = summoner["summonerId"]!.GetValue<long>().ToString(CultureInfo.InvariantCulture);
        var path = $"lol-item-sets/v1/item-sets/{summonerId}/sets";

        var document = await lcu.GetAsync(path, ct) as JsonObject ?? new JsonObject { ["accountId"] = summoner["accountId"]?.DeepClone(), ["itemSets"] = new JsonArray() };
        Merge(document, request, time.GetUtcNow());
        await lcu.PutAsync(path, document, ct);
        return ImportResult.Ok($"Item set « {request.Title} » ajouté (boutique en jeu → Objets recommandés).");
    }

    internal static string Uid(int championId) => UidPrefix + championId.ToString(CultureInfo.InvariantCulture);

    internal static void Merge(JsonObject document, ItemSetRequest request, DateTimeOffset now)
    {
        if (document["itemSets"] is not JsonArray sets)
        {
            sets = [];
            document["itemSets"] = sets;
        }

        var uid = Uid(request.ChampionId);
        foreach (var old in sets.OfType<JsonObject>().Where(s => s["uid"]?.GetValue<string>() == uid).ToList())
        {
            sets.Remove(old);
        }

        sets.Add(new JsonObject
        {
            ["uid"] = uid,
            ["title"] = request.Title,
            ["type"] = "custom",
            ["map"] = "SR",
            ["mode"] = "CLASSIC",
            ["sortrank"] = 0,
            ["startedFrom"] = "blank",
            ["associatedChampions"] = new JsonArray(request.ChampionId),
            ["associatedMaps"] = new JsonArray(SummonersRift),
            ["preferredItemSlots"] = new JsonArray(),
            ["blocks"] = new JsonArray(request.Blocks.Select(b => (JsonNode)new JsonObject
            {
                ["type"] = b.Title,
                ["items"] = new JsonArray(b.Items.Where(i => i > 0)
                    .Select(i => (JsonNode)new JsonObject { ["id"] = i.ToString(CultureInfo.InvariantCulture), ["count"] = 1 }).ToArray()),
            }).ToArray()),
        });
        document["timestamp"] = now.ToUnixTimeMilliseconds();
    }
}
