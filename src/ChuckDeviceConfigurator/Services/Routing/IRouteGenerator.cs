namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    /// <summary>
    /// Route generator
    /// </summary>
    public interface IRouteGenerator
    {
        /// <summary>
        ///     Generates a route using the specified route generator options.
        /// </summary>
        /// <param name="options">Route generator options to use.</param>
        /// <returns>Returns a list of coordinates of the generated route.</returns>
        List<Coordinate> GenerateRoute(RouteGeneratorOptions options);
    }
}