namespace ChuckDeviceController.PluginManager.Services.Finder
{
    public interface IPluginFinderOptions
    {
        /// <summary>
        /// Gets or sets the root directory to search for plugins.
        /// </summary>
        string RootPluginsDirectory { get; }

        /// <summary>
        /// Gets or sets the underlying type of the plugin object.
        /// </summary>
        Type PluginType { get; }

        /// <summary>
        /// Gets or sets the valid plugin file types to search for.
        /// Default: *.dll
        /// </summary>
        IEnumerable<string> ValidFileTypes { get; }

        //uint MaxRecursionDepth { get; }
    }
}