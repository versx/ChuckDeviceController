namespace ChuckDeviceController.Common.Tasks
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Task interface contract describing what is expected
    /// for job controller task instances.
    /// </summary>
    public interface ITask
    {
        /// <summary>
        /// Gets the action type the device is expected to
        /// perform.
        /// </summary>
        [JsonPropertyName("action")]
        string Action { get; }

        /// <summary>
        /// Gets the latitude coordinate of where the device
        /// will teleport to next to perform this job task.
        /// </summary>
        [JsonPropertyName("lat")]
        double Latitude { get; }

        /// <summary>
        /// Gets the longitude coordinate of where the device
        /// will teleport to next to perform this job task.
        /// </summary>
        [JsonPropertyName("lon")]
        double Longitude { get; }

        /// <summary>
        /// Gets the minimum level of the account the device
        /// should be logged into.
        /// </summary>
        [JsonPropertyName("min_level")]
        ushort MinimumLevel { get; }

        /// <summary>
        /// Gets the maximum level of the account the device
        /// should be logged into.
        /// </summary>
        [JsonPropertyName("max_level")]
        ushort MaximumLevel { get; }
    }
}