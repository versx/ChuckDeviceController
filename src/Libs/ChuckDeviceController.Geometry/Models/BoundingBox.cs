namespace ChuckDeviceController.Geometry.Models
{
    public class BoundingBox
    {
        public double MinimumLatitude { get; set; } // minX

        public double MaximumLatitude { get; set; } // maxX

        public double MinimumLongitude { get; set; } // minY

        public double MaximumLongitude { get; set; } // maxY

        public bool IsInBoundingBox(double lat, double lon)
        {
            var result = 
                lat >= MinimumLatitude && lon >= MinimumLongitude &&
                lat <= MaximumLatitude && lon <= MaximumLongitude;
            return result;
        }
    }
}