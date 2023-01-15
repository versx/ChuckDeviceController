namespace ChuckDeviceController.Common;

using System.ComponentModel;
using System.Text.Json.Serialization;

public class GeofenceData : Dictionary<string, object?>
{
    [
        DisplayName("Area"),
        JsonPropertyName("area"),
    ]
    public dynamic? Area { get; set; }
}