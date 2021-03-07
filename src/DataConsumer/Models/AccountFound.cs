namespace DataConsumer.Models
{
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    public class AccountFound
    {
        [JsonPropertyName("gpr")]
        public GetPlayerOutProto Player { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }
}