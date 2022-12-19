namespace RobotsPlugin.Data.Abstractions;

using Models;

/// <summary>
///     Interface contract used to create a robots.txt file to
///     allow or deny web crawler robots/spiders to our routes.
/// </summary>
public interface IRobots
{
    /// <summary>
    ///     List of known user agents.
    /// </summary>
    /// <returns>
    ///     Returns a <seealso cref="List{string}"/> of all
    ///     configured user agent strings.
    /// </returns>
    IEnumerable<string> UserAgents { get; }

    /// <summary>
    ///     Returns a list of all custom user defined denied routes.
    /// </summary>
    /// <returns>
    ///     Returns a <seealso cref="List{DeniedRoute}"/> of all
    ///     configured denied routes.
    /// </returns>
    IEnumerable<DeniedRoute> DeniedRoutes { get; }

    /// <summary>
    ///     Returns a list of all custom user defined routes.
    /// </summary>
    /// <returns>
    ///     Returns a <seealso cref="List{IRobotRouteData}"/> of all
    ///     configured routes.
    /// </returns>
    IEnumerable<IRobotRouteData> CustomRoutes { get; }

    /// <summary>
    ///     Adds a custom agent to the list of current user agents.
    /// </summary>
    /// <param name="userAgent">Name of the user agent.</param>
    /// <returns>
    ///     Returns <code>true</code> if the user agent was added successfully,
    ///     otherwise <code>false</code>.
    /// </returns>
    bool AddUserAgent(string userAgent);

    /// <summary>
    ///     Removes a custom user agent from the list of current user agents.
    /// </summary>
    /// <param name="userAgent">Name of the user agent.</param>
    /// <returns>
    ///     Returns <code>true</code> if the user agent was removed successfully,
    ///     otherwise <code>false</code>.
    /// </returns>
    bool RemoveUserAgent(string userAgent);

    /// <summary>
    ///     Returns all data on allowed and denied routes for an user agent
    /// </summary>
    /// <param name="userAgent">Name of the user agent.</param>
    /// <returns>
    ///     Returns a <seealso cref="List{string}"/> of configured routes for the
    ///     specified user agent string.
    /// </returns>
    IEnumerable<string> GetRoutes(string userAgent);

    /// <summary>
    ///     Adds a route to the custom list based on the provided parameters.
    /// </summary>
    /// <param name="userAgent">Name of the user agent.</param>
    /// <param name="route">Route which will be denied.</param>
    /// <param name="isAllowed">Whether the route is allowed or denied.</param>
    /// <param name="comment"></param>
    /// <returns>
    ///     Returns <code>true</code> if the route was added successfully,
    ///     otherwise <code>false</code>.
    /// </returns>
    bool AddRoute(string userAgent, string route, bool isAllowed, string? comment = null);

    /// <summary>
    ///     Updates a previously added allowed or denied route, if found.
    /// </summary>
    /// <param name="userAgent">Name of the user agent.</param>
    /// <param name="route">Route that will be removed.</param>
    /// <param name="isAllowed">Whether the route is allowed or denied.</param>
    /// <param name="comment"></param>
    /// <returns>
    ///     Returns <code>true</code> if the route was updated successfully,
    ///     otherwise <code>false</code>.
    /// </returns>
    bool UpdateRoute(string userAgent, string route, bool isAllowed, string? comment = null);

    /// <summary>
    ///     Removes a previously added allowed or denied route, if found.
    /// </summary>
    /// <param name="userAgent">Name of the user agent.</param>
    /// <param name="route">Route that will be removed.</param>
    /// <returns>Returns <code>true</code> if the route was removed.</returns>
    bool RemoveRoute(string userAgent, string route);

    /// <summary>
    ///     Saves the web crawler robots config data.
    /// </summary>
    /// <returns>
    ///     Returns <code>true</code> if the data was saved successfully,
    ///     otherwise <code>false</code>.
    /// </returns>
    bool SaveData();
}