namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("geofence")]
    public class Geofence : BaseEntity
    {
        #region Properties

        [

            Column("name"),
            Key,
        ]
        public string Name { get; set; }

        [
            Column("type"),
            Required,
        ]
        public GeofenceType Type { get; set; }

        [
            Column("data"),
            Required,
        ]
        public GeofenceData Data { get; set; }

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
}