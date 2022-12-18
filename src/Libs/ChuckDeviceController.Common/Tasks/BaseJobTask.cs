namespace ChuckDeviceController.Common.Tasks
{
    using System.Text.Json.Serialization;

    public abstract class BaseJobTask : ITask
    {
        [JsonPropertyName("action")]
        public virtual string Action { get; set; } = null!;

        [JsonPropertyName("lat")]
        public virtual double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public virtual double Longitude { get; set; }

        [JsonPropertyName("min_level")]
        public virtual ushort MinimumLevel { get; set; }

        [JsonPropertyName("max_level")]
        public virtual ushort MaximumLevel { get; set; }
    }
}