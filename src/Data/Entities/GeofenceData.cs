namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.Text.Json.Serialization;

    public class GeofenceData
    {
        [JsonPropertyName("area")]
        public dynamic Area { get; set; }
    }
}