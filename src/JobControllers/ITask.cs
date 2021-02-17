namespace ChuckDeviceController.JobControllers
{
    using System.Text.Json.Serialization;

    public interface ITask
    {
        [JsonPropertyName("area")]
        public string Area { get; }

        [JsonPropertyName("action")]
        public string Action { get; }

        [JsonPropertyName("lat")]
        public double Latitude { get; }

        [JsonPropertyName("lon")]
        public double Longitude { get; }

        [JsonPropertyName("min_level")]
        public ushort MinimumLevel { get; }

        [JsonPropertyName("max_level")]
        public ushort MaximumLevel { get; }
    }
}