namespace ChuckDeviceController.Geometry.Models
{
    using System.Text.Json.Serialization;

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