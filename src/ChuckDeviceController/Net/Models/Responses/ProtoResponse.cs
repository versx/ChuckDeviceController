namespace ChuckDeviceController.Net.Models.Responses
{
    using System.Text.Json.Serialization;

    public class ProtoResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public ProtoDataDetails Data { get; set; }
    }

    public class ProtoDataDetails
    {
        [JsonPropertyName("nearby")]
        public int Nearby { get; set; }

        [JsonPropertyName("wild")]
        public int Wild { get; set; }

        [JsonPropertyName("forts")]
        public int Forts { get; set; }

        [JsonPropertyName("quests")]
        public int Quests { get; set; }

        [JsonPropertyName("fort_search")]
        public int FortSearch { get; set; }

        [JsonPropertyName("encounters")]
        public int Encounters { get; set; }

        [JsonPropertyName("level")]
        public ushort Level { get; set; }

        [JsonPropertyName("only_empty_gmos")]
        public bool OnlyEmptyGmos { get; set; }

        [JsonPropertyName("only_invalid_gmos")]
        public bool OnlyInvalidGmos { get; set; }

        [JsonPropertyName("contains_gmos")]
        public bool ContainsGmos { get; set; }

        [JsonPropertyName("in_area")]
        public bool InArea { get; set; }

        [JsonPropertyName("lat_target")]
        public double? LatitudeTarget { get; set; }

        [JsonPropertyName("lon_target")]
        public double? LongitudeTarget { get; set; }

        [JsonPropertyName("pokemon_lat")]
        public double? PokemonLatitude { get; set; }

        [JsonPropertyName("pokemon_lon")]
        public double? PokemonLongitude { get; set; }

        [JsonPropertyName("pokemon_encounter_id")]
        public string? PokemonEncounterId { get; set; }
    }
}
