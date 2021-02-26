namespace Chuck.Infrastructure.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using Chuck.Infrastructure.Data.Interfaces;

    [Table("geofence")]
    public class Geofence : BaseEntity, IAggregateRoot
    {
        [
            Column("name"),
            Key,
            JsonPropertyName("name"),
        ]
        public string Name { get; set; }

        [
            Column("type"),
            JsonPropertyName("type"),
        ]
        public GeofenceType Type { get; set; }

        [
            Column("data"),
            JsonPropertyName("data"),
        ]
        public GeofenceData Data { get; set; }

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
    }

    // TODO: Deserialize by name
    public enum GeofenceType
    {
        Circle = 0,
        Geofence,
    }
}