using RiftLens.Domain.ValueObjects;

namespace RiftLens.Domain.Tests;

public class RiotIdTests
{
    [Theory]
    [InlineData("Faker#KR1", "Faker", "KR1")]
    [InlineData("  Le Gazo #EUW ", "Le Gazo", "EUW")]
    [InlineData("a#b#TAG", "a#b", "TAG")]
    public void TryParse_accepte_les_formats_valides(string input, string name, string tag)
    {
        Assert.True(RiotId.TryParse(input, out var id));
        Assert.Equal(new RiotId(name, tag), id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SansTag")]
    [InlineData("#EUW")]
    [InlineData("Nom#")]
    [InlineData("Nom#AB")]
    [InlineData("Nom#ABCDEF")]
    [InlineData("UnNomBeaucoupTropLong#EUW")]
    public void TryParse_rejette_les_formats_invalides(string? input) => Assert.False(RiotId.TryParse(input, out _));
}
