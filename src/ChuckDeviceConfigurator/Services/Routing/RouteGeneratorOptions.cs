namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    /// <summary>
    /// Routing generation options
    /// </summary>
    public class RouteGeneratorOptions
    {
        /// <summary>
        /// Gets or sets the list of <seealso cref="MultiPolygon"/> (geofences)
        /// used to generate the route in.
        /// </summary>
        public List<MultiPolygon> MultiPolygons { get; set; } = new();

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
        public double CircleSize { get; set; } = Strings.DefaultCircleSize;
    }
}