namespace RobotsPlugin.Attributes
{
    /// <summary>
    /// The deny robot attribute is used on Controller Action methods to indicate that
    /// a web crawler robot should not use that particular route.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class DenyRobotAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// The user agent that is to be denied access to the route.
        /// </summary>
        /// <value>string</value>
        public string UserAgent { get; private set; }

        /// <summary>
        /// Optional comment that will appear in the robots.txt file.
        /// </summary>
        /// <value>string</value>
        public string Comment { get; private set; }

        /// <summary>
        /// Route associated with the Deny attribute
        /// </summary>
        public string Route { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor, indicates that all user agents are denied.
        /// </summary>
        public DenyRobotAttribute()
            : this("*")
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userAgent">Specify the specific user agent that is to be denied access to the route.</param>
        public DenyRobotAttribute(string userAgent)
            : this(userAgent, string.Empty)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userAgent">Specify the specific user agent that is to be denied access to the route.</param>
        /// <param name="comment">Comment to be included in the automatically generated robots.txt file.</param>
        /// <exception cref="ArgumentNullException">Raised if userAgent is null or empty.</exception>
        public DenyRobotAttribute(string userAgent, string comment)
        {
            if (string.IsNullOrEmpty(userAgent))
                throw new ArgumentNullException(nameof(userAgent));

            UserAgent = userAgent;
            Comment = comment;
        }

        #endregion
    }
}