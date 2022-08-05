namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Plugins;

    public interface IPluginManager
    {
        #region Properties

        IReadOnlyDictionary<string, IPlugin> Plugins { get; }

        string PluginsFolder { get; }

        #endregion

        #region Methods

        Task LoadPluginsAsync();

        Task LoadPluginsAsync(IEnumerable<string> pluginFilePaths);

        // TODO: StartAsync

        Task StopAsync(string pluginName);

        Task StopAllAsync();

        Task ReloadAsync(string pluginName);

        Task ReloadAllAsync();

        Task RemoveAsync(string pluginName);

        Task RemoveAllAsync();

        #endregion
    }
}