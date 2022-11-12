namespace ChuckDeviceController.Geometry.Models
{
    using System.Collections.Generic;

    using ChuckDeviceController.Common.Geometry;

    public class Polygon : List<double>, IPolygon
    {
        public Polygon(double lat, double lon)
        {
            AddRange(new[] { lat, lon });
        }

        public Coordinate ToCoordinate()
        {
            return new Coordinate(this.FirstOrDefault(), this.LastOrDefault());
        }
    }
}