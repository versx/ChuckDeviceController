namespace ChuckDeviceController.Routing.Optimization;

/// <summary>
/// Routing optimization options
/// </summary>
public class RouteOptimizerOptions
{
    /// <summary>
    /// Gets or sets a value used to determine the radius
    /// between each coordinate when generating the route.
    /// </summary>
    public ushort RadiusM { get; set; } = Strings.DefaultRadiusM;

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