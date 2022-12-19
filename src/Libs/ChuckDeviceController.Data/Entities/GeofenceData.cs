namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Data.Abstractions;

public class GeofenceData : IGeofenceData
{
    [
        DisplayName("Area"),
        Column("area"),
        JsonPropertyName("area"),
    ]
    public dynamic? Area { get; set; }
}