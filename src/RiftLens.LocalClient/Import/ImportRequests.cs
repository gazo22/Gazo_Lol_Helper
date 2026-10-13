namespace RiftLens.LocalClient.Import;

/// <param name="Perks">6 runes puis 3 fragments (offensif, flexible, défensif), ordre attendu par le client.</param>
public sealed record RunePageRequest(string Name, int PrimaryStyleId, int SubStyleId, IReadOnlyList<int> Perks);

public sealed record ItemSetBlock(string Title, IReadOnlyList<int> Items);

public sealed record ItemSetRequest(string Title, int ChampionId, IReadOnlyList<ItemSetBlock> Blocks);

public sealed record ImportResult(bool Success, string Message)
{
    public static ImportResult Ok(string message) => new(true, message);

    public static ImportResult Fail(string message) => new(false, message);
}
