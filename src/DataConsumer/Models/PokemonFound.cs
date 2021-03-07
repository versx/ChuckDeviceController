namespace DataConsumer.Models
{
    using System.Text.Json.Serialization;

    public class PokemonFound<T>
    {
        [JsonPropertyName("cell")]
        public ulong CellId { get; set; }

        [JsonPropertyName("data")]
        public T Pokemon { get; set; }

        [JsonPropertyName("timestamp_ms")]
        public ulong TimestampMs { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }
}