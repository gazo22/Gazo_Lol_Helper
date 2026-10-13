namespace RiftLens.UI.Components;

/// <summary>Construction centralisée des URLs internes.</summary>
public static class Routes
{
    public static string Profile(string gameName, string tagLine) =>
        $"profile/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";

    public static string Champion(string championKey) => $"champions/{Uri.EscapeDataString(championKey)}";
}
