namespace RiftLens.UI.Components;

/// <summary>Échelle sémantique de couleur des winrates (rouge → neutre → vert).</summary>
public static class WinrateColor
{
    public static string For(double winrate) => winrate switch
    {
        < 0.47 => "var(--rl-wr-low)",
        < 0.49 => "color-mix(in srgb, var(--rl-wr-low) 50%, var(--rl-wr-mid))",
        <= 0.51 => "var(--rl-wr-mid)",
        <= 0.53 => "color-mix(in srgb, var(--rl-wr-high) 50%, var(--rl-wr-mid))",
        _ => "var(--rl-wr-high)",
    };
}
