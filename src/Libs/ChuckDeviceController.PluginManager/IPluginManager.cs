namespace ChuckDeviceController.PluginManager;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;

public interface IPluginManager
{
    #region Properties

    IReadOnlyDictionary<string, IPluginHost> Plugins { get; }

    IPluginHost? this[string key] { get; }

    IPluginManagerOptions Options { get; }

    IServiceCollection Services { get; }

    IWebHostEnvironment WebHostEnv { get; }

    #endregion

    #region Events

    event EventHandler<PluginHostAddedEventArgs>? PluginHostAdded;

    event EventHandler<PluginHostRemovedEventArgs>? PluginHostRemoved;

    event EventHandler<PluginHostStateChangedEventArgs>? PluginHostStateChanged;

    #endregion

    #region Methods

    void Configure(WebApplication appBuilder);

    Task<IServiceCollection> LoadPluginsAsync(
        IServiceCollection services,
        IWebHostEnvironment env,
        Func<IReadOnlyList<IApiKey>> apiKeysFunc,
        Func<IReadOnlyList<IPluginState>> pluginsFunc,
        ServiceProvider serviceProvider);

    Task LoadPluginAsync(string filePath, Func<IReadOnlyList<IApiKey>> apiKeysFunc, ServiceProvider serviceProvider);

    Task RegisterPluginAsync(PluginHost pluginHost);

    Task StopAsync(string pluginName);

    Task StopAllAsync();

    Task ReloadAsync(string pluginName);

    Task ReloadAllAsync();

    Task RemoveAsync(string pluginName, bool unload = true);

    Task RemoveAllAsync();

    Task SetStateAsync(string pluginName, PluginState state);

    #endregion
}