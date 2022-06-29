namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    public class GeofenceData
    {
        [
            Column("area"),
            JsonPropertyName("area"),
        ]
        public dynamic Area { get; set; }
    }
}