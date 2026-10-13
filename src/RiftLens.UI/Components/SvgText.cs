using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace RiftLens.UI.Components;

/// <summary>Élément SVG &lt;text&gt; (balise réservée par Razor, d'où ce composant).</summary>
public sealed class SvgText : ComponentBase
{
    [Parameter] public string X { get; set; } = "0";
    [Parameter] public string Y { get; set; } = "0";
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Value { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "x", X);
        builder.AddAttribute(2, "y", Y);
        builder.AddAttribute(3, "class", Class);
        builder.AddContent(4, Value);
        builder.CloseElement();
    }
}
