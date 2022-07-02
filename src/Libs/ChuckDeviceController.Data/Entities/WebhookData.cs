namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    public class WebhookData
    {
        [
            DisplayName("Pokemon IDs"),
            JsonPropertyName("pokemon_ids"),
        ]
        public List<uint> PokemonIds { get; set; }

        [
            DisplayName("Pokestop IDs"),
            JsonPropertyName("pokestop_ids"),
        ]
        public List<string> PokestopIds { get; set; }

        [
            DisplayName("Raid Pokemon IDs"),
            JsonPropertyName("raid_ids"),
        ]
        public List<uint> RaidPokemonIds { get; set; }

        [
            DisplayName("Raid Egg Levels"),
            JsonPropertyName("egg_levels"),
        ]
        public List<ushort> EggLevels { get; set; }

        [
            DisplayName("Pokestop Lure IDs"),
            JsonPropertyName("lure_ids"),
        ]
        public List<ushort> LureIds { get; set; }

        [
            DisplayName("Invasion Grunt IDs"),
            JsonPropertyName("invasion_ids"),
        ]
        public List<ushort> InvasionIds { get; set; }

        [
            DisplayName("Gym IDs"),
            JsonPropertyName("gym_ids"),
        ]
        public List<ushort> GymTeamIds { get; set; }

        [
            DisplayName("Weather Condition IDs"),
            JsonPropertyName("weather_ids"),
        ]
        public List<ushort> WeatherConditionIds { get; set; }
    }
}