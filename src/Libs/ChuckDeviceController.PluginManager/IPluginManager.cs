namespace ChuckDeviceController.PluginManager
{
    using Microsoft.AspNetCore.Builder;

    using ChuckDeviceController.Common.Data;

    public interface IPluginManager
    {
        #region Properties

        IReadOnlyDictionary<string, IPluginHost> Plugins { get; }

        IPluginHost? this[string key] { get; }

        IPluginManagerOptions Options { get; }

        #endregion

        #region Events

        event EventHandler<PluginHostAddedEventArgs>? PluginHostAdded;

        event EventHandler<PluginHostRemovedEventArgs>? PluginHostRemoved;

        event EventHandler<PluginHostStateChangedEventArgs>? PluginHostStateChanged;

        #endregion

        #region Methods

        void Configure(WebApplication appBuilder);

        Task RegisterPluginAsync(PluginHost pluginHost);

        Task StopAsync(string pluginName);

        Task StopAllAsync();

        Task ReloadAsync(string pluginName);

        Task ReloadAllAsync();

        Task RemoveAsync(string pluginName);

        Task RemoveAllAsync();

        Task SetStateAsync(string pluginName, PluginState state);

        IEnumerable<string> GetPluginFolderNames();

        #endregion
    }
}