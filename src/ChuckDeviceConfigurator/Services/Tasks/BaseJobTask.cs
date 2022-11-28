namespace ChuckDeviceConfigurator.Services.Tasks
{
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Tasks;

    public class BaseJobTask : ITask
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = null!;

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("min_level")]
        public ushort MinimumLevel { get; set; }

        [JsonPropertyName("max_level")]
        public ushort MaximumLevel { get; set; }
    }
}