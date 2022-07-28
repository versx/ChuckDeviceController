namespace ChuckDeviceController.Net.Models.Responses
{
    using System.Text.Json.Serialization;

    public class DeviceAssignmentResponse
    {
        [JsonPropertyName("assigned")]
        public bool Assigned { get; set; }

        [JsonPropertyName("first_warning_timestamp")]
        public ulong? FirstWarningTimestamp { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("commit")]
        public string? Commit { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }
    }
}