namespace ChuckDeviceController.Geometry.Models
{
    using System.Collections.Generic;

    public class Polygon : List<double>
    {
        public Polygon(double lat, double lon)
        {
            AddRange(new[] { lat, lon });
        }
    }
}