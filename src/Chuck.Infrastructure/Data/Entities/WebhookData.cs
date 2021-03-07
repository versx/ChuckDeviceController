namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class WebhookData
    {
        [JsonPropertyName("pokemon_ids")]
        public List<uint> PokemonIds { get; set; }

        [JsonPropertyName("pokestop_ids")]
        public List<string> PokestopIds { get; set; }

        [JsonPropertyName("raid_ids")]
        public List<uint> RaidPokemonIds { get; set; }

        [JsonPropertyName("egg_levels")]
        public List<ushort> EggLevels { get; set; }

        [JsonPropertyName("lure_ids")]
        public List<ushort> LureIds { get; set; }

        [JsonPropertyName("invasion_ids")]
        public List<ushort> InvasionIds { get; set; }

        [JsonPropertyName("gym_ids")]
        public List<ushort> GymTeamIds { get; set; }

        [JsonPropertyName("weather_ids")]
        public List<ushort> WeatherConditionIds { get; set; }
    }
}