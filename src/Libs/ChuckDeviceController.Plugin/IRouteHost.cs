namespace ChuckDeviceController.Plugin
{
    using ChuckDeviceController.Common.Geometry;

    /// <summary>
    /// 
    /// </summary>
    public interface IRouteHost
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        List<ICoordinate> GenerateRoute(RouteGeneratorOptions options);
    }

    /// <summary>
    /// Routing generation type
    /// </summary>
    public enum RouteGenerationType
    {
        /// <summary>
        /// Generates a bootstrap route based on the
        /// circle size.
        /// </summary>
        Bootstrap,

        /// <summary>
        /// Generates a randomized route
        /// </summary>
        Randomized,

        /// <summary>
        /// Generates an optimized route
        /// </summary>
        Optimized,
    }

    /// <summary>
    /// Routing generation options
    /// </summary>
    public class RouteGeneratorOptions
    {
        /// <summary>
        /// Gets or sets the list of <seealso cref="IMultiPolygon"/> (geofences)
        /// used to generate the route in.
        /// </summary>
        public List<IMultiPolygon> MultiPolygons { get; set; } = new();

        /// <summary>
        /// Gets or sets the route generation type to use.
        /// </summary>
        public RouteGenerationType RouteType { get; set; } = RouteGenerationType.Randomized;

        /// <summary>
        /// Gets or sets a value to limit the amount of coordinate
        /// points to return when generating the route.
        /// </summary>
        public uint MaximumPoints { get; set; } = 500;

        /// <summary>
        /// Gets or sets a value used to determine the size of each
        /// coordinate to space between when generating the route.
        /// </summary>
        public double CircleSize { get; set; } = 70;// Strings.DefaultCircleSize;
    }
}