namespace RobotsPlugin.Data.Models;

/// <summary>
/// Contains information on routes that are denied to robots
/// </summary>
public sealed class DeniedRoute
{
    #region Properties

    /// <summary>
    /// Route being denied
    /// </summary>
    public string Route { get; private set; }

    /// <summary>
    /// User agent name being denied
    /// </summary>
    public string UserAgent { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="route"></param>
    /// <param name="userAgent"></param>
    /// <exception cref="ArgumentNullException">Raised if route is null or empty</exception>
    /// <exception cref="ArgumentNullException">Raised if userAgent is null or empty</exception>
    /// <exception cref="ArgumentException">Raised if route is not a valid partial uri</exception>
    public DeniedRoute(string route, string userAgent)
    {
        if (string.IsNullOrEmpty(route))
            throw new ArgumentNullException(nameof(route));

        if (string.IsNullOrEmpty(userAgent))
            throw new ArgumentNullException(nameof(userAgent));

        if (!Uri.TryCreate(route, UriKind.Relative, out _))
            throw new ArgumentException("route must be a partial Uri", nameof(route));

        Route = route;
        UserAgent = userAgent;
    }

    #endregion
}