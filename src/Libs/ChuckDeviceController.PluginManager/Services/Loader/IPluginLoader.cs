namespace ChuckDeviceController.PluginManager.Services.Loader;

/// <summary>
/// 
/// </summary>
public interface IPluginLoader
{
    event EventHandler<PluginLoadedEventArgs> PluginLoaded;

    /// <summary>
    /// 
    /// </summary>
    IEnumerable<PluginHost> LoadedPlugins { get; }
}