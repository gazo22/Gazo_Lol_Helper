using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace RiftLens.LocalClient.Connection;

/// <summary>Sous-ensemble du protocole WAMP 1 utilisé par le WebSocket du client LoL.</summary>
public static class WampMessage
{
    public const int Subscribe = 5;
    public const int Unsubscribe = 6;
    public const int Event = 8;

    public const string ChampSelectTopic = "OnJsonApiEvent_lol-champ-select_v1_session";
    public const string GameflowTopic = "OnJsonApiEvent_lol-gameflow_v1_gameflow-phase";

    public static string SubscribeTo(string topic) => JsonSerializer.Serialize(new object[] { Subscribe, topic });

    /// <summary>Analyse <c>[8, "topic", {"data": …, "eventType": "Update", "uri": "…"}]</c>.</summary>
    public static bool TryParseEvent(string message, [NotNullWhen(true)] out LcuEvent? lcuEvent)
    {
        lcuEvent = null;
        try
        {
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 3
                || root[0].ValueKind != JsonValueKind.Number || root[0].GetInt32() != Event
                || root[2].ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var payload = root[2];
            lcuEvent = new LcuEvent(
                root[1].GetString() ?? string.Empty,
                payload.TryGetProperty("uri", out var uri) ? uri.GetString() ?? string.Empty : string.Empty,
                payload.TryGetProperty("eventType", out var type) ? type.GetString() ?? string.Empty : string.Empty,
                payload.TryGetProperty("data", out var data) ? data.Clone() : default);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

/// <param name="EventType">Create, Update ou Delete.</param>
public sealed record LcuEvent(string Topic, string Uri, string EventType, JsonElement Data);
