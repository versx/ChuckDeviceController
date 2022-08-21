namespace ChuckDeviceController.PluginManager.Services.Loader
{
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.PluginManager.Services.Loader.Runtime;

    /// <summary>
    /// Plugin assembly's loading context where all dependants and 
    /// references will be loaded.
    /// </summary>
    public interface IPluginAssemblyLoadContext
    {
        string AssemblyFullPath { get; }

        Type PluginType { get; }

        IEnumerable<Type> HostTypes { get; }

        IEnumerable<string> HostAssemblies { get; }

        IServiceCollection HostServices { get; }

        string HostFramework { get; }

        IEnumerable<Type> DowngradableHostTypes { get; }

        IEnumerable<string> DowngradableHostAssemblies { get; }

        IEnumerable<Type> RemoteTypes { get; }

        PluginPlatformVersion PluginPlatformVersion { get; }

        IEnumerable<string> AdditionalProbingPaths { get; }

        bool IgnorePlatformInconsistencies { get; }

        void Unload();
    }
}