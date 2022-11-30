namespace ChuckDeviceController.Geometry.Models
{
    using ChuckDeviceController.Geometry.Models.Contracts;

    public class Polygon : List<double>, IPolygon
    {
        public Polygon(double lat, double lon)
        {
            AddRange(new[] { lat, lon });
        }

        public ICoordinate ToCoordinate()
        {
            return new Coordinate(this.FirstOrDefault(), this.LastOrDefault());
        }
    }
}