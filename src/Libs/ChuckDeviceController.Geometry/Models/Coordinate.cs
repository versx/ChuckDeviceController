namespace ChuckDeviceController.Geometry.Models
{
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common.Geometry;

    public class Coordinate : ICoordinate
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

        public int CompareTo(object? obj)
        {
            if (obj == null)
                return -1;

            var other = (Coordinate)obj;

            var latResult = Latitude.CompareTo(other.Latitude);
            if (latResult != 0)
            {
                return latResult;
            }

            var lonResult = Longitude.CompareTo(other.Longitude);
            if (lonResult != 0)
            {
                return lonResult;
            }

            return 0;
        }

        public override string ToString()
        {
            return $"{Latitude},{Longitude}";
        }
    }
}