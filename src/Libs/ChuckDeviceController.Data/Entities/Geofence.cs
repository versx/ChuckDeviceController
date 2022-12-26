namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;

[Table("geofence")]
public class Geofence : BaseEntity, IGeofence
{
    #region Properties

    [
        DisplayName("Name"),
        Column("name"),
        Key,
        JsonPropertyName("name"),
    ]
    public string Name { get; set; } = null!;

    [
        DisplayName("Type"),
        Column("type"),
        Required,
        JsonPropertyName("type"),
    ]
    public GeofenceType Type { get; set; }

    [
        DisplayName("Data"),
        Column("data"),
        //Required,
        JsonPropertyName("data"),
        //JsonExtensionData,
    ]
    public GeofenceData? Data { get; set; }

    [
        DisplayName("Count"),
        NotMapped,
        JsonIgnore,
    ]
    public uint AreasCount { get; set; }

    #endregion

    #region Helper Methods

    public static string GeofenceTypeToString(GeofenceType type)
    {
        return type switch
        {
            GeofenceType.Circle => "circle",
            GeofenceType.Geofence => "geofence",
            _ => type.ToString(),
        };
    }

    public static GeofenceType StringToGeofenceType(string geofenceType)
    {
        return (geofenceType.ToLower()) switch
        {
            "circle" => GeofenceType.Circle,
            "geofence" => GeofenceType.Geofence,
            _ => GeofenceType.Circle,
        };
    }

    #endregion
}