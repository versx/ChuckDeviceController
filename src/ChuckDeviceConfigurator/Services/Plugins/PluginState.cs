namespace ChuckDeviceConfigurator.Services.Plugins
{
    /// <summary>
    /// Determines the state of a plugin
    /// </summary>
    public enum PluginState
    {
        /// <summary>
        /// Plugin state has not be set yet
        /// </summary>
        Unset = 0,

        /// <summary>
        /// Plugin is currently running and active
        /// </summary>
        Running,

        /// <summary>
        /// Plugin has been stopped and is not currently running
        /// </summary>
        Stopped,

        /// <summary>
        /// Plugin has encountered an error and unable to recover
        /// </summary>
        Error,
    }
}