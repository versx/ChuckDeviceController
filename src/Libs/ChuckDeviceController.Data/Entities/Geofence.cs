﻿namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;

    [Table("geofence")]
    public class Geofence : BaseEntity, IGeofence
    {
        #region Properties

        [
            DisplayName("Name"),
            Column("name"),
            Key,
        ]
        public string Name { get; set; }

        [
            DisplayName("Geofence Type"),
            Column("type"),
            Required,
        ]
        public GeofenceType Type { get; set; }

        [
            DisplayName("Data"),
            Column("data"),
            Required,
        ]
        public GeofenceData Data { get; set; }

        [

            DisplayName("Count"),
            NotMapped,
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
}