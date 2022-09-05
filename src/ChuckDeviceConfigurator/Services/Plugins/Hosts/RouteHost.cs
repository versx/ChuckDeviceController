namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Common.Geometry;
    using ChuckDeviceController.Plugin;

    public class RouteHost : IRouteHost
    {
        public List<ICoordinate> GenerateRoute(RouteGeneratorOptions options)
        {
            return new();
        }
    }
}