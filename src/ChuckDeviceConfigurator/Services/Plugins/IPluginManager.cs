namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Plugins;

    public interface IPluginManager
    {
        #region Properties

        IReadOnlyDictionary<string, PluginHost> Plugins { get; }

        string PluginsFolder { get; }

        #endregion

        #region Methods

        Task LoadPluginsAsync(IReadOnlyDictionary<Type, object> sharedHosts);

        Task LoadPluginsAsync(IEnumerable<string> pluginFilePaths, IReadOnlyDictionary<Type, object> sharedHosts);

        Task StopAsync(string pluginName);

        Task StopAllAsync();

        Task ReloadAsync(string pluginName);

        Task ReloadAllAsync();

        Task RemoveAsync(string pluginName);

        Task RemoveAllAsync();

        #endregion
    }
}