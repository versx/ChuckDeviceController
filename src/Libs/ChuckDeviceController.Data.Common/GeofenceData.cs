namespace ChuckDeviceController.Data.Common;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

public class GeofenceData : Dictionary<string, object>
{
    [
        DisplayName("Area"),
        Column("area"),
        JsonPropertyName("area"),
    ]
    public dynamic? Area { get; set; }
}