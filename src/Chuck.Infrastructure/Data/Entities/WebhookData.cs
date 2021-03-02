namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

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
        public List<Item> LureIds { get; set; }

        [JsonPropertyName("invasion_ids")]
        public List<InvasionCharacter> InvasionIds { get; set; }

        [JsonPropertyName("gym_ids")]
        public List<Team> GymTeamIds { get; set; }

        [JsonPropertyName("weather_ids")]
        public List<WeatherCondition> WeatherConditionIds { get; set; }
    }
}