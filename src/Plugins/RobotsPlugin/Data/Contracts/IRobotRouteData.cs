namespace RobotsPlugin.Data.Contracts
{
    /// <summary>
    ///     Interface contract for managing custom web crawler robot route data.
    /// </summary>
    public interface IRobotRouteData
    {
        /// <summary>
        ///     Gets or sets the UserAgent name.
        /// </summary>
        /// <value>Returns the UserAgent string.</value>
        public string UserAgent { get; set; }

        /// <summary>
        ///     Gets or sets a custom comment applied to the user defined route data.
        /// </summary>
        /// <value>Returns the comment for the route.</value>
        public string? Comment { get; set; }

        /// <summary>
        ///     Gets or sets the name of the user defined route.
        /// </summary>
        /// <value>Returns the route name.</value>
        public string Route { get; set; }

        /// <summary>
        ///     Gets or sets a value determining whether it is an allowed or denied route.
        /// </summary>
        /// <value>Returns <code>true</code> if allowed, otherwise <code>false</code>.</value>
        public bool IsAllowed { get; set; }

        /// <summary>
        ///     Gets or sets a value determineing whether the route is a custom user defined
        ///     route, or one discovered via attributes.
        /// </summary>
        /// <value>Returns <code>true</code> if custom, otherwise <code>false</code>.</value>
        public bool IsCustom { get; set; }
    }
}