namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    /// <summary>
    /// Routing optimization options
    /// </summary>
    public class RouteOptimizerOptions
    {
        /// <summary>
        /// Gets or sets a value used to determine the size of each
        /// coordinate to space between when optimizing the route.
        /// </summary>
        public ushort CircleSize { get; set; } = Strings.DefaultCircleSize;

        /// <summary>
        /// Gets or sets a value used to decide how many optimization
        /// attempts are made to find the shortest route possible.
        /// </summary>
        public ushort OptimizationAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value indicating whether to optimize the
        /// route solving the travelling salesman problem to find
        /// the shortest route possible between coordinates.
        /// </summary>
        public bool OptimizeTsp { get; set; }
    }
}