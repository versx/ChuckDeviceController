namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Routing generation type
    /// </summary>
    public enum RouteGenerationType
    {
        /// <summary>
        /// Generates a bootstrap route based on the
        /// circle size.
        /// </summary>
        Bootstrap,

        /// <summary>
        /// Generates a randomized route
        /// </summary>
        Randomized,

        /// <summary>
        /// Generates an optimized route
        /// </summary>
        Optimized,
    }
}