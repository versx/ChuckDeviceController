namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Geometry;

    /// <summary>
    /// 
    /// </summary>
    public interface IRouteHost
    {
        /// <summary>
        ///     Generates a route using the specified route generator options.
        /// </summary>
        /// <param name="options">Route generator options to use.</param>
        /// <returns>Returns a list of coordinates of the generated route.</returns>
        List<ICoordinate> GenerateRoute(RouteGeneratorOptions options);
    }
}