namespace Chuck.Common.JobControllers.Tasks
{
    using System.Text.Json.Serialization;

    public class IVTask : ITask
    {
        [JsonPropertyName("area")]
        public string Area { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("min_level")]
        public ushort MinimumLevel { get; set; }

        [JsonPropertyName("max_level")]
        public ushort MaximumLevel { get; set; }

        [JsonPropertyName("id")]
        public ulong EncounterId { get; set; }

        [JsonPropertyName("is_spawnpoint")]
        public bool IsSpawnpoint { get; set; }

        public IVTask()
        {
            Action = ActionType.ScanIV;
        }
    }
}