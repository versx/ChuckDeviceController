﻿namespace Chuck.Infrastructure.Data.Entities
{
    using System.Text.Json.Serialization;

    // TODO: Move to Geofence.Models namespace
    public class Coordinate
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        public Coordinate()
        {
        }

        public Coordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public override string ToString()
        {
            return $"{Latitude},{Longitude}";
        }
    }
}