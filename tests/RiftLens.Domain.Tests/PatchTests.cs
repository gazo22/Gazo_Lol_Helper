using RiftLens.Domain.ValueObjects;

namespace RiftLens.Domain.Tests;

public class PatchTests
{
    [Theory]
    [InlineData("15.20.715.1234", 15, 20)]
    [InlineData("15.3", 15, 3)]
    public void TryParse_extrait_majeur_mineur(string version, int major, int minor)
    {
        Assert.True(Patch.TryParse(version, out var patch));
        Assert.Equal(new Patch(major, minor), patch);
    }

    [Fact]
    public void Comparaison_numerique_et_non_lexicale() => Assert.True(new Patch(15, 10) > new Patch(15, 9));

    [Theory]
    [InlineData("abc")]
    [InlineData("15")]
    [InlineData(null)]
    public void TryParse_rejette_les_versions_invalides(string? version) => Assert.False(Patch.TryParse(version, out _));
}
