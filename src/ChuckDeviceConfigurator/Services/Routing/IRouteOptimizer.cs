namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models;

    /// <summary>
    /// Route optimizer
    /// </summary>
    public interface IRouteOptimizer
    {
        /// <summary>
        /// Gets or sets a value determining whether gyms are included or considered
        /// in the route optimization.
        /// </summary>
        bool IncludeGyms { get; }

        /// <summary>
        /// Gets or sets a value determining whether pokestops are included or considered
        /// in the route optimization.
        /// </summary>
        bool IncludePokestops { get; }

        /// <summary>
        /// Gets or sets a value determining whether spawnpoints are included or considered
        /// in the route optimization.
        /// </summary>
        bool IncludeSpawnpoints { get; }

        /// <summary>
        /// Gets or sets a value determining whether S2 cells are included or considered
        /// in the route optimization.
        /// </summary>
        bool IncludeS2Cells { get; }

        /// <summary>
        /// Gets or sets a value determining whether Pokemon nests are included or considered
        /// in the route optimization.
        /// </summary>
        bool IncludeNests { get; }

        /*
        /// <summary>
        /// Gets or sets a value determining whether the polygons are optimized in the route
        /// optimization.
        /// </summary>
        bool OptimizePolygons { get; }

        /// <summary>
        /// Gets or sets a value determining whether circle points are optimized in the route
        /// optimization.
        /// </summary>
        bool OptimizeCircles { get; }
        */

        /// <summary>
        /// Gets or sets the multi polygons to use for optimizing the route.
        /// </summary>
        IReadOnlyList<MultiPolygon> MultiPolygons { get; }

        /// <summary>
        /// Optimizes the provided route using the specified route optimizer options.
        /// </summary>
        /// <param name="options">Route optimizer options to use.</param>
        /// <returns>Returns a list of optimized route coordinates.</returns>
        Task<List<Coordinate>> OptimizeRouteAsync(RouteOptimizerOptions options);
    }
}
