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
        public IEnumerable<ActiveEventItem> Spawns { get; set; }

        [DisplayName("Hatchable Eggs")]
        public IEnumerable<ActiveEventItem> Eggs { get; set; }

        [DisplayName("Raid Bosses")]
        public IEnumerable<ActiveEventRaidItem> Raids { get; set; }

        [DisplayName("Shiny Pokemon")]
        public IEnumerable<ActiveEventItem> Shinies { get; set; }

        [DisplayName("Bonuses")]
        public IEnumerable<BonusItem> Bonuses { get; set; }

        public IEnumerable<ActiveEventItem> Features { get; set; }

        [DisplayName("Has Quests")]
        [JsonPropertyName("has_quests")]
        public bool HasQuests { get; set; }

        [DisplayName("Has Spawnpoints Increase")]
        [JsonPropertyName("has_spawnpoints")]
        public bool HasSpawnpoints { get; set; }
    }
}