namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceController.Common.Data;

    public interface IPluginManager
    {
        #region Properties

        IReadOnlyDictionary<string, IPluginHost> Plugins { get; }

        IPluginHost this[string key] { get; }

        IPluginManagerOptions Options { get; }

        #endregion

        #region Methods

        Task RegisterPluginAsync(PluginHost pluginHost);

        Task StopAsync(string pluginName);

        Task StopAllAsync();

        Task ReloadAsync(string pluginName);

        Task ReloadAllAsync();

        Task RemoveAsync(string pluginName);

        Task RemoveAllAsync();

        Task SetStateAsync(string pluginName, PluginState state);

        #endregion
    }
}