namespace ChuckDeviceController.Plugin;

using ChuckDeviceController.Geometry.Models.Abstractions;

/// <summary>
/// Route generator plugin host.
/// </summary>
public interface IRoutingHost
{
    /// <summary>
    ///     Generates a route using the specified route generator options.
    /// </summary>
    /// <param name="options">Routing generation options to use.</param>
    /// <returns>Returns a list of coordinates of the generated route.</returns>
    List<ICoordinate> GenerateRoute(RouteGeneratorOptions options);

    // TODO: OptimizeRoute

    // TODO: Clusters
}