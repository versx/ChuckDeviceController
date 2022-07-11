namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    public class RouteGeneratorOptions
    {
        public List<MultiPolygon> MultiPolygons { get; set; } = new();

        public RouteGenerationType RouteType { get; set; } = RouteGenerationType.Randomized;

        public uint MaximumPoints { get; set; } = 500;

        public double CircleSize { get; set; } = Strings.DefaultCircleSize;
    }
}