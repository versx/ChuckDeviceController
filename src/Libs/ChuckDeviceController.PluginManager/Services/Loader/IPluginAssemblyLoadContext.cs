namespace ChuckDeviceController.PluginManager.Loader
{
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.PluginManager.Services.Loader.Runtime;

    public interface IPluginAssemblyLoadContext
    {
        string FullAssemblyPath { get; }

        Type PluginType { get; }

        IEnumerable<Type> HostTypes { get; }

        IEnumerable<string> HostAssemblies { get; }

        IEnumerable<Type> DowngradableHostTypes { get; }

        IEnumerable<string> DowngradableHostAssemblies { get; }

        IEnumerable<Type> RemoteTypes { get; }

        PluginPlatformVersion PluginPlatformVersion { get; }

        IEnumerable<string> AdditionalProbingPaths { get; }

        IServiceCollection HostServices { get; }

        string HostFramework { get; }
    }
}