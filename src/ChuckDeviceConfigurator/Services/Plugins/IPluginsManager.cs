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
        // TODO: ReloadAsync

        Task StopAsync(string pluginName);

        Task StopAllAsync();

        Task UnloadAsync(string pluginName);

        Task UnloadAllAsync();

        #endregion
    }
}