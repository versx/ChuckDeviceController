namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    public class RouteOptimizerOptions
    {
        public ushort CircleSize { get; set; } = Strings.DefaultCircleSize;

        public ushort OptimizationAttempts { get; set; } = 3;

        public bool OptimizeTsp { get; set; }
    }
}