namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Plugins;

    public interface IPluginLoader<TPlugin> where TPlugin : class, IPlugin
    {
        IEnumerable<IPlugin> LoadedPlugins { get; }
    }
}