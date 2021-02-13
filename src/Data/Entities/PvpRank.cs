namespace ChuckDeviceController.Data.Entities
{
    using System.Text.Json.Serialization;

    public class PvpRank
    {
        [JsonPropertyName("cp")]
        public ushort CP { get; set; }

        [JsonPropertyName("rank")]
        public ushort Rank { get; set; }

        [JsonPropertyName("pokemon")]
        public ushort Pokemon { get; set; }

        [JsonPropertyName("form")]
        public ushort Form { get; set; }

        [JsonPropertyName("level")]
        public ushort Level { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }
    }
}