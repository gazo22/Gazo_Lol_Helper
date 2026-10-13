using System.Text.Json.Nodes;
using RiftLens.LocalClient.Connection;

namespace RiftLens.LocalClient.Import;

/// <summary>
/// Crée une page de runes RiftLens et la sélectionne. L'ancienne page RiftLens est remplacée ;
/// les pages du joueur ne sont jamais supprimées.
/// </summary>
public static class RunePageImporter
{
    public const string NamePrefix = "RiftLens";
    private const string PagesPath = "lol-perks/v1/pages";

    public static async Task<ImportResult> ImportAsync(LcuHttpClient lcu, RunePageRequest request, CancellationToken ct = default)
    {
        if (request.Perks.Count != 9 || request.PrimaryStyleId <= 0 || request.SubStyleId <= 0)
        {
            return ImportResult.Fail("Page de runes incomplète.");
        }

        var pages = await lcu.GetAsync(PagesPath, ct) as JsonArray ?? [];
        foreach (var page in pages.OfType<JsonObject>().Where(IsOurs))
        {
            await lcu.DeleteAsync($"{PagesPath}/{(long)page["id"]!}", ct);
        }

        try
        {
            await lcu.PostAsync(PagesPath, BuildPayload(request), ct);
        }
        catch (LcuRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Forbidden)
        {
            return ImportResult.Fail("Aucun emplacement de page libre : supprimez une page de runes dans le client puis réessayez.");
        }

        return ImportResult.Ok($"Page « {Truncate(request.Name)} » importée et sélectionnée.");
    }

    internal static JsonObject BuildPayload(RunePageRequest request) => new()
    {
        ["name"] = Truncate(request.Name),
        ["primaryStyleId"] = request.PrimaryStyleId,
        ["subStyleId"] = request.SubStyleId,
        ["selectedPerkIds"] = new JsonArray(request.Perks.Select(p => (JsonNode)p).ToArray()),
        ["current"] = true,
    };

    private static bool IsOurs(JsonObject page) =>
        page["name"]?.GetValue<string>()?.StartsWith(NamePrefix, StringComparison.Ordinal) == true
        && page["isDeletable"]?.GetValue<bool>() != false;

    /// <summary>Le client limite les noms de page à 25 caractères.</summary>
    private static string Truncate(string name) => name.Length <= 25 ? name : name[..25];
}
