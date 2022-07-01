namespace ChuckDeviceConfigurator.Net.Models.Responses
{
    using System.Text.Json.Serialization;

    public class DeviceAssignmentResponse
    {
        [JsonPropertyName("assigned")]
        public bool Assigned { get; set; }

        [JsonPropertyName("first_warning_timestamp")]
        public ulong? FirstWarningTimestamp { get; set; }
    }
}