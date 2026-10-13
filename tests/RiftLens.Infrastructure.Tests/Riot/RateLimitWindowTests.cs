using RiftLens.Infrastructure.Riot.RateLimit;

namespace RiftLens.Infrastructure.Tests.Riot;

public class RateLimitWindowTests
{
    [Fact]
    public void ParseHeader_lit_le_format_Riot()
    {
        var windows = RateLimitWindow.ParseHeader("20:1,100:120");
        Assert.Equal([new RateLimitWindow(20, TimeSpan.FromSeconds(1)), new RateLimitWindow(100, TimeSpan.FromSeconds(120))], windows);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0:1")]
    public void ParseHeader_ignore_les_valeurs_invalides(string? header) => Assert.Empty(RateLimitWindow.ParseHeader(header));
}
