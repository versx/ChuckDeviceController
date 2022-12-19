namespace ChuckDeviceController.PluginManager.Services.Loader;

using Microsoft.Extensions.DependencyInjection;

using ChuckDeviceController.PluginManager.Services.Loader.Runtime;

/// <summary>
/// Plugin assembly's loading context where all dependants and 
/// references will be loaded.
/// </summary>
public interface IPluginAssemblyLoadContext
{
    #region Properties

    /// <summary>
    /// Gets or sets the full file path of the plugin assembly.
    /// </summary>
    string AssemblyFullPath { get; }

    /// <summary>
    /// Gets or sets the generic type of the plugin assembly.
    /// </summary>
    Type PluginType { get; }

    /// <summary>
    /// Gets or sets the generic type collection for the host
    /// application.
    /// </summary>
    IEnumerable<Type> HostTypes { get; }

    /// <summary>
    /// Gets or sets the host application's referenced assemblies.
    /// </summary>
    IEnumerable<string> HostAssemblies { get; }

    /// <summary>
    /// Gets or sets the host application's services collection.
    /// </summary>
    IServiceCollection HostServices { get; }

    /// <summary>
    /// Gets or sets the host application's runtime framework.
    /// </summary>
    string HostFramework { get; }

    /// <summary>
    /// Gets or sets the host application's downgradable host
    /// application's services.
    /// </summary>
    IEnumerable<Type> DowngradableHostTypes { get; }

    /// <summary>
    /// Gets or sets the host application's downgradable referenced
    /// assemblies.
    /// </summary>
    IEnumerable<string> DowngradableHostAssemblies { get; }

    IEnumerable<Type> RemoteTypes { get; }

    PluginPlatformVersion PluginPlatformVersion { get; }

    IEnumerable<string> AdditionalProbingPaths { get; }

    bool IgnorePlatformInconsistencies { get; }

    bool IsLoaded { get; }

    #endregion

    #region Methods

    void Load();

    void Unload();

    void Reload();

    #endregion
}