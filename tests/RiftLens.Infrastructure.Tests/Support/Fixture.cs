using System.Text.Json;
using RiftLens.Infrastructure.Riot.Dtos;

namespace RiftLens.Infrastructure.Tests.Support;

internal static class Fixture
{
    public static string Read(string relativePath) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath));

    public static MatchDto Match() => JsonSerializer.Deserialize(Read("riot/match.json"), RiotJsonContext.Default.MatchDto)!;

    public static TimelineDto Timeline() => JsonSerializer.Deserialize(Read("riot/timeline.json"), RiotJsonContext.Default.TimelineDto)!;

    public static JsonDocument Doc(string relativePath) => JsonDocument.Parse(Read(relativePath));
}
