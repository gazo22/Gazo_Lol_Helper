using Microsoft.AspNetCore.Components;
using RiftLens.UI.Layout;

namespace RiftLens.UI.Tests;

public sealed class ErrorBoundaryTests : BunitContext
{
    private sealed class Boom : ComponentBase
    {
        protected override void OnInitialized() => throw new InvalidOperationException("Profil illisible");
    }

    [Fact]
    public void Une_page_en_erreur_affiche_un_message_au_lieu_de_planter()
    {
        var cut = Render<AppErrorBoundary>(p => p.AddChildContent<Boom>());

        Assert.Contains("Cette page a rencontré une erreur", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Profil illisible", cut.Markup, StringComparison.Ordinal);
    }
}
