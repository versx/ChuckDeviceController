namespace PogoEventsPlugin.Models;

using System.Text.Json.Serialization;

public class EventItem : IEventItem
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }
}