namespace ChuckDeviceController.Plugins
{
    // TODO: Add support for custom IJobControllers
    // TODO: Add support for Configurator to inherit shared library for custom services registration

    // NOTE: Only used for DI testing purposes
    public interface IPluginService
    {
        string Test { get; }
    }

    /// <summary>
    /// Base Plugin interface contract all plugins will inherit
    /// at a minimum.
    /// </summary>
    public interface IPlugin : IMetadata, IWebPlugin
    {
        /// <summary>
        /// 
        /// </summary>
        Task InitializeAsync();
    }
}