namespace PogoEventsPlugin.Models;

using System.Text.Json.Serialization;

public class EventBonusItem : IEventBonusItem
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("template")]
    public string? Template { get; set; }
}