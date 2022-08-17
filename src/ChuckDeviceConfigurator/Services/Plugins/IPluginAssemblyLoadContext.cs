﻿namespace ChuckDeviceConfigurator.Services.Plugins
{
    using ChuckDeviceConfigurator.Services.Plugins.Mvc.Extensions;

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