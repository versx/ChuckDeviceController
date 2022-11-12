namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Base Plugin interface contract all plugins are required to
    /// inherit at a minimum.
    /// </summary>
    public interface IPlugin : IMetadata, IWebPlugin, IPluginEvents // REVIEW: Possibly make 'IWebPlugin' optional
    {
    }
}