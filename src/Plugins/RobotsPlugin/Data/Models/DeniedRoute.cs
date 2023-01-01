namespace RobotsPlugin.Data.Models;

/// <summary>
/// Contains information on routes that are denied to robots
/// </summary>
public sealed class DeniedRoute
{
    #region Properties

    /// <summary>
    /// 
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Route being denied
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// User agent name being denied
    /// </summary>
    public string UserAgent { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="id"></param>
    /// <param name="route"></param>
    /// <param name="userAgent"></param>
    /// <exception cref="ArgumentNullException">Raised if route is null or empty</exception>
    /// <exception cref="ArgumentNullException">Raised if userAgent is null or empty</exception>
    /// <exception cref="ArgumentException">Raised if route is not a valid partial uri</exception>
    public DeniedRoute(Guid id, string route, string userAgent)
    {
        if (string.IsNullOrEmpty(route))
            throw new ArgumentNullException(nameof(route));

        if (string.IsNullOrEmpty(userAgent))
            throw new ArgumentNullException(nameof(userAgent));

        if (!Uri.TryCreate(route, UriKind.Relative, out _))
            throw new ArgumentException("route must be a partial Uri", nameof(route));

        Id = id;
        Route = route;
        UserAgent = userAgent;
    }

    #endregion
}