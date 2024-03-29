﻿namespace PogoEventsPlugin.Models;

using System.ComponentModel;
using System.Text.Json.Serialization;

public class ActiveEvent : IActiveEvent
{
    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    [DisplayName("Starts")]
    public string Start { get; set; } = null!;

    [DisplayName("Ends")]
    public string End { get; set; } = null!;

    [DisplayName("Wild Pokemon Spawns")]
    public IEnumerable<EventItem> Spawns { get; set; } = Array.Empty<EventItem>();

    [DisplayName("Hatchable Eggs")]
    public IEnumerable<EventItem> Eggs { get; set; } = Array.Empty<EventItem>();

    [DisplayName("Raid Bosses")]
    public IEnumerable<EventRaidItem> Raids { get; set; } = Array.Empty<EventRaidItem>();

    [DisplayName("Shiny Pokemon")]
    public IEnumerable<EventItem> Shinies { get; set; } = Array.Empty<EventItem>();

    [DisplayName("Bonuses")]
    public IEnumerable<EventBonusItem> Bonuses { get; set; } = Array.Empty<EventBonusItem>();

    [DisplayName("Features")]
    public IEnumerable<string> Features { get; set; } = Array.Empty<string>();

    [
        DisplayName("Has Event Quests"),
        JsonPropertyName("has_quests"),
    ]
    public bool HasQuests { get; set; }

    [
        DisplayName("Has Spawnpoints Increase"),
        JsonPropertyName("has_spawnpoints"),
    ]
    public bool HasSpawnpoints { get; set; }

    [
        DisplayName("Is Active"),
        JsonIgnore,
    ]
    public bool IsActive =>
        // If Start date/time set, check if current date is greater than Start date
        (Start != null && DateTime.Parse(Start) <= DateTime.UtcNow) ||
        // or if Start date is not set, check if End date is set and hasn't lapsed yet.
        // Probably not a good idea to assume event started just because start date is not set but ...
        (string.IsNullOrEmpty(Start) && End != null && DateTime.Parse(End) >= DateTime.UtcNow);
}