namespace DataConsumer.Models
{
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    public class FortFound
    {
        [JsonPropertyName("cell")]
        public ulong CellId { get; set; }

        [JsonPropertyName("data")]
        public PokemonFortProto Fort { get; set; }
    }
}