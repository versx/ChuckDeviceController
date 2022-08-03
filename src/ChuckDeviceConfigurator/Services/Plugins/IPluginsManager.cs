namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Plugins;

    public interface IPluginsManager
    {
        IReadOnlyDictionary<string, IPlugin> Plugins { get; }

        Task LoadPluginsAsync(string pluginFolder);
    }
}