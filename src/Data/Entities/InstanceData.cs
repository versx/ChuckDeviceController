namespace ChuckDeviceController.Data.Entities
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class InstanceData
    {
        [JsonPropertyName("is_event")]
        public bool IsEvent { get; set; }

        [JsonPropertyName("min_level")]
        public ushort MinimumLevel { get; set; }

        [JsonPropertyName("max_level")]
        public ushort MaximumLevel { get; set; }

        [JsonPropertyName("timezone_offset")]
        public int? TimezoneOffset { get; set; }

        [JsonPropertyName("area")]
        public dynamic Area { get; set; }

        [JsonPropertyName("iv_queue_limit")]
        public ushort? IVQueueLimit { get; set; }

        [JsonPropertyName("pokemon_ids")]
        public List<uint> PokemonIds { get; set; }

        [JsonPropertyName("spin_limit")]
        public ushort? SpinLimit { get; set; }

        //[JsonPropertyName("scatter_pokemon_ids")]
        //public List<uint> ScatterPokemonIds { get; set; }

        //[JsonPropertyName("account_group")]
        //public string AccountGroup { get; set; }

        // radius
        // store_data
        // delay_logout (GD crap?)
    }
}