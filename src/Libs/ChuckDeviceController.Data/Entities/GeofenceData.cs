namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    public class GeofenceData
    {
        [
            DisplayName("Area"),
            Column("area"),
            JsonPropertyName("area"),
        ]
        public dynamic Area { get; set; }
    }
}