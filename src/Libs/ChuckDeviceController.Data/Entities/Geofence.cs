namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Extensions.Json.Converters;
using ChuckDeviceController.Geometry.Models;

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
        JsonConverter(typeof(ObjectDataConverter<GeofenceData>)),
    ]
    public GeofenceData? Data { get; set; }

    [
        DisplayName("Count"),
        NotMapped,
        JsonIgnore,
    ]
    public uint AreasCount
    {
        get
        {
            string area = Convert.ToString(Data?.Area);
            if (string.IsNullOrEmpty(area))
                return 0;

            var count = Type == GeofenceType.Circle
                ? area?.FromJson<List<Coordinate>>()?.Count ?? 0
                : area?.FromJson<List<List<Coordinate>>>()?.Count ?? 0;
            return (uint)count;
        }
    }

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