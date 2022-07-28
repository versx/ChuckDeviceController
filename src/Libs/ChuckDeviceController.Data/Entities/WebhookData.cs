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
        public List<uint> PokemonIds { get; set; } = new();

        [
            DisplayName("Pokestop IDs"),
            JsonPropertyName("pokestop_ids"),
        ]
        public List<string> PokestopIds { get; set; } = new();

        [
            DisplayName("Raid Pokemon IDs"),
            JsonPropertyName("raid_ids"),
        ]
        public List<uint> RaidPokemonIds { get; set; } = new();

        [
            DisplayName("Raid Egg Levels"),
            JsonPropertyName("egg_levels"),
        ]
        public List<ushort> EggLevels { get; set; } = new();

        [
            DisplayName("Pokestop Lure IDs"),
            JsonPropertyName("lure_ids"),
        ]
        public List<ushort> LureIds { get; set; } = new();

        [
            DisplayName("Invasion Grunt IDs"),
            JsonPropertyName("invasion_ids"),
        ]
        public List<ushort> InvasionIds { get; set; } = new();

        [
            DisplayName("Gym IDs"),
            JsonPropertyName("gym_ids"),
        ]
        public List<ushort> GymTeamIds { get; set; } = new();

        [
            DisplayName("Weather Condition IDs"),
            JsonPropertyName("weather_ids"),
        ]
        public List<ushort> WeatherConditionIds { get; set; } = new();
    }
}