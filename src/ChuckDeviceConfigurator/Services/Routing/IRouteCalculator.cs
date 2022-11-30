namespace ChuckDeviceConfigurator.Services.Routing
{
    using ChuckDeviceController.Geometry.Models.Contracts;

    /// <summary>
    /// Route calculator to find the shortest routing path in order
    /// to prevent unwanted big jumps.
    /// </summary>
    public interface IRouteCalculator
    {
        /// <summary>
        ///     Gets or sets a value determining whether the coordinates list
        ///     is cleared after the route calculator has finished optimizing
        ///     the route.
        /// </summary>
        bool ClearCoordinatesAfterOptimization { get; set; }

        /// <summary>
        ///     Gets or sets the list of coordinates to include when calculating
        ///     the optimized route.
        /// </summary>
        IReadOnlyList<ICoordinate> Coordinates { get; }


        /// <summary>
        ///     Adds the specified coordinate to the coordinates list.
        /// </summary>
        /// <param name="coordinate">Coordinate to add to the list.</param>
        void AddCoordinate(ICoordinate coordinate);

        /// <summary>
        ///     Adds a list of coordinates to the coordinates list.
        /// </summary>
        /// <param name="coordinates">
        ///     List of coordinates to add to the list.
        /// </param>
        void AddCoordinates(List<ICoordinate> coordinates);

        /// <summary>
        ///     Clears all current coordinates in the coordinates list.
        /// </summary>
        void ClearCoordinates();

        /// <summary>
        ///     Calculates the shortest route for the provided route coordinates.
        /// </summary>
        /// <returns>
        ///     Returns a queue of coordinates with the shortest routing path to
        ///     prevent big jumps.
        /// </returns>
        Queue<ICoordinate> CalculateShortestRoute();
    }
}