namespace RobotsPlugin.Data.Abstractions
{
    /// <summary>
    ///     Interface contract for managing custom web crawler robot route data.
    /// </summary>
    public interface IRobotRouteData
    {
        /// <summary>
        /// 
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     Gets or sets the UserAgent name.
        /// </summary>
        /// <value>Returns the UserAgent string.</value>
        string UserAgent { get; }

        /// <summary>
        ///     Gets or sets a custom comment applied to the user defined route data.
        /// </summary>
        /// <value>Returns the comment for the route.</value>
        string? Comment { get; }

        /// <summary>
        ///     Gets or sets the name of the user defined route.
        /// </summary>
        /// <value>Returns the route name.</value>
        string Route { get; }

        /// <summary>
        ///     Gets or sets a value determining whether it is an allowed or denied route.
        /// </summary>
        /// <value>Returns <c>true</c> if allowed, otherwise <c>false</c>.</value>
        bool IsAllowed { get; }

        /// <summary>
        ///     Gets or sets a value determineing whether the route is a custom user defined
        ///     route, or one discovered via attributes.
        /// </summary>
        /// <value>Returns <c>true</c> if custom, otherwise <c>false</c>.</value>
        bool IsCustom { get; }
    }
}