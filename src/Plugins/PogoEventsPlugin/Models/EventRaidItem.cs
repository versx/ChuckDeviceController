namespace PogoEventsPlugin.Models;

using System.Text.Json.Serialization;

public class EventRaidItem : IEventRaidItem
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("form")]
    public uint? Form { get; set; }

    [JsonPropertyName("temp_evolution_id")]
    public uint? TempEvolutionId { get; set; }
}