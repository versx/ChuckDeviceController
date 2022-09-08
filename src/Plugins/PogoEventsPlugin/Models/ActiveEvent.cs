namespace PogoEventsPlugin.Models
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    public class ActiveEvent : IActiveEvent
    {
        public string Name { get; set; }

        public string Type { get; set; }

        [DisplayName("Starts")]
        public string Start { get; set; }

        [DisplayName("Ends")]
        public string End { get; set; }

        [DisplayName("Wild Pokemon Spawns")]
        public IEnumerable<EventItem> Spawns { get; set; }

        [DisplayName("Hatchable Eggs")]
        public IEnumerable<EventItem> Eggs { get; set; }

        [DisplayName("Raid Bosses")]
        public IEnumerable<EventRaidItem> Raids { get; set; }

        [DisplayName("Shiny Pokemon")]
        public IEnumerable<EventItem> Shinies { get; set; }

        [DisplayName("Bonuses")]
        public IEnumerable<EventBonusItem> Bonuses { get; set; }

        [DisplayName("Features")]
        public IEnumerable<string> Features { get; set; }

        [
            DisplayName("Has Quests"),
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
}