namespace PogoEventsPlugin.Models;

using System.Text.Json.Serialization;

public class EventQuestItem : IEventQuestItem
{
    [JsonPropertyName("task")]
    public string Task { get; set; } = null!;

    [JsonPropertyName("rewards")]
    public List<EventQuestReward> Rewards { get; set; } = new();
}

public class EventQuestReward : IEventQuestReward
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("amount")]
    public uint? Amount { get; set; }

    [JsonPropertyName("id")]
    public uint? Id { get; set; }

    [JsonPropertyName("reward")]
    public EventQuestRewardItem? Reward { get; set; }
}

public class EventQuestRewardItem : IEventQuestRewardItem
{
    [JsonPropertyName("form")]
    public uint? Form { get; set; }

    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }
}