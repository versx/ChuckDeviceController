namespace RobotsPlugin.Attributes
{
    /// <summary>
    /// The deny robot attribute is used on Controller Action methods to indicate that
    /// a web crawler robot should not use that particular route.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class DenyRobotAttribute : Attribute
    {
        private const string DenyAllUserAgents = "*";

        #region Properties

        /// <summary>
        /// Gets the user agent that is to be denied access to the route.
        /// </summary>
        public string UserAgent { get; }

        /// <summary>
        /// Gets the route associated with the Deny attribute.
        /// </summary>
        public string Route { get; internal set; }

        /// <summary>
        /// Gets the optional comment that will appear in the robots.txt file.
        /// </summary>
        public string Comment { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor, indicates that all user agents are denied.
        /// </summary>
        public DenyRobotAttribute()
            : this(DenyAllUserAgents)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userAgent">Specify the specific user agent that is to be denied access to the route.</param>
        public DenyRobotAttribute(string userAgent)
            : this(userAgent, string.Empty)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userAgent">Specify the specific user agent that is to be denied access to the route.</param>
        /// <param name="route"></param>
        /// <param name="comment">Comment to be included in the automatically generated robots.txt file.</param>
        /// <exception cref="ArgumentNullException">Raised if userAgent is null or empty.</exception>
        public DenyRobotAttribute(string userAgent, string route, string? comment = null)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new ArgumentNullException(nameof(userAgent));
            }

            UserAgent = userAgent;
            Route = route;
            Comment = comment ?? string.Empty;
        }

        #endregion
    }
}