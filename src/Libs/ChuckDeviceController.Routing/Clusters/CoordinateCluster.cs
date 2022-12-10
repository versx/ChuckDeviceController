namespace ChuckDeviceController.Routing.Clusters
{
    using ChuckDeviceController.Geometry.Models.Contracts;

    public class CoordinateCluster
    {
        public CoordinateType Type { get; set; }

        public ICoordinate Coordinate { get; set; } = null!;

        public CoordinateCluster()
        {
        }

        public CoordinateCluster(CoordinateType type, ICoordinate coordinate)
        {
            Type = type;
            Coordinate = coordinate;
        }
    }
}