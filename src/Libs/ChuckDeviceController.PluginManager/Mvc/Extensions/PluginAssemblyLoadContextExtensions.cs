namespace ChuckDeviceController.PluginManager.Mvc.Extensions
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.PluginManager.Services.Loader;
    using ChuckDeviceController.PluginManager.Services.Loader.Runtime;

    public static class PluginAssemblyLoadContextExtensions
    {
        public static PluginAssemblyLoadContext AddMvcTypes(this PluginAssemblyLoadContext loadContext)
        {
            return loadContext.AddHostTypes(new[]
            {
                typeof(ControllerBase),
            });
        }

        public static PluginAssemblyLoadContext AddMvcRazorTypes(this PluginAssemblyLoadContext loadContext)
        {
            return loadContext
                .AddMvcTypes()
                .AddHostTypes(new[]
                {
                    typeof(ControllerBase),
                    typeof(ITempDataDictionaryFactory),
                });
        }

        public static PluginAssemblyLoadContext AddHostServices(this PluginAssemblyLoadContext loadContext,
               IServiceCollection hostServices,
               IEnumerable<Type>? includeTypes = null,
               IEnumerable<Type>? excludeTypes = null)
        {
            if (!(includeTypes?.Any() ?? false))
                return loadContext; // short circuit

            var hostTypes = new List<Type>();
            var chuckServices = hostServices.Where(s => IsChuckService(s.ServiceType));
            var includeServices = hostServices.Where(s => Includes(s.ServiceType, includeTypes));
            var excludeServices = hostServices.Where(s => Excludes(s.ServiceType, excludeTypes));

            foreach (var hostService in hostServices
                                        .Except(chuckServices)
                                        .Union(includeServices)
                                        .Except(excludeServices))
            {
                loadContext.AddHostService(hostService);
            }

            return loadContext;
        }

        public static PluginAssemblyLoadContext AddHostService(this PluginAssemblyLoadContext loadContext, Type hostServiceType, Type hostServiceImplementationType, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            return loadContext.AddHostService(new ServiceDescriptor(hostServiceType, hostServiceImplementationType, serviceLifetime));
        }

        public static PluginAssemblyLoadContext AddHostService<T>(this PluginAssemblyLoadContext loadContext, T implementation, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        {
            return loadContext.AddHostService(new ServiceDescriptor(typeof(T), (s) => implementation, serviceLifetime));
        }

        public static PluginAssemblyLoadContext AddHostService(this PluginAssemblyLoadContext loadContext, ServiceDescriptor hostService)
        {
            // Add the Host service to the servicecollection of the plugin
            loadContext.HostServices.Add(hostService);

            return loadContext
                  // A host type will always live inside the host
                  .AddHostTypes(new[] { hostService.ServiceType })
                  // The implementation type will always exist on the Host, since it will be created here
                  .AddHostTypes(new[]
                  {
                      hostService.ImplementationType ??
                      hostService.ImplementationInstance?.GetType() ??
                      hostService.ImplementationFactory?.Method.ReturnType
                  });
        }

        public static PluginAssemblyLoadContext AddHostTypes(this PluginAssemblyLoadContext loadContext, IEnumerable<Type> hostTypes)
        {
            if (!(hostTypes?.Any() ?? false))
                return loadContext; // short circuit

            loadContext.HostTypes = new List<Type>(loadContext.HostTypes.Union(hostTypes));
            return loadContext;
        }

        public static PluginAssemblyLoadContext AddHostAssemblies(this PluginAssemblyLoadContext loadContext, IEnumerable<string> assemblies)
        {
            if (!(assemblies?.Any() ?? false))
                return loadContext; // short circuit

            loadContext.HostAssemblies = new List<string>(loadContext.HostAssemblies.Union(assemblies));
            return loadContext;
        }

        public static PluginAssemblyLoadContext AddRemoteTypes(this PluginAssemblyLoadContext loadContext, IEnumerable<Type> remoteTypes)
        {
            if (!(remoteTypes?.Any() ?? false))
                return loadContext; // short circuit

            loadContext.RemoteTypes = new List<Type>(loadContext.RemoteTypes.Union(remoteTypes));
            return loadContext;
        }

        public static PluginAssemblyLoadContext AddDowngradableHostTypes(this PluginAssemblyLoadContext loadContext, IEnumerable<Type> downgradableHostTypes)
        {
            if (!(downgradableHostTypes?.Any() ?? false))
                return loadContext; // short circuit

            loadContext.DowngradableHostTypes = new List<Type>(loadContext.DowngradableHostTypes.Union(downgradableHostTypes));
            return loadContext;
        }

        public static PluginAssemblyLoadContext AddDowngradableHostAssemblies(this PluginAssemblyLoadContext loadContext, IEnumerable<string> assemblies)
        {
            if (!(assemblies?.Any() ?? false))
                return loadContext; // short circuit

            loadContext.DowngradableHostAssemblies = new List<string>(loadContext.DowngradableHostAssemblies.Union(assemblies));
            return loadContext;
        }

        public static PluginAssemblyLoadContext AddAdditionalProbingPaths(this PluginAssemblyLoadContext loadContext, IEnumerable<string> additionalProbingPaths)
        {
            if (!(additionalProbingPaths?.Any() ?? false))
                return loadContext; // short circuit

            loadContext.AdditionalProbingPaths = new List<string>(loadContext.AdditionalProbingPaths.Union(additionalProbingPaths));
            return loadContext;
        }

        public static PluginAssemblyLoadContext SetPlatformVersion(this PluginAssemblyLoadContext loadContext, PluginPlatformVersion pluginPlatformVersion)
        {
            loadContext.PluginPlatformVersion = pluginPlatformVersion;
            return loadContext;
        }

        private static bool IsChuckService(Type type) =>
        (
            (type?.Namespace?.StartsWith("ChuckDeviceController.") ?? false) ||
            (type?.Namespace?.StartsWith("ChuckDeviceConfigurator.") ?? false) ||
            (type?.Namespace?.StartsWith("ChuckDeviceCommunicator.") ?? false)
        );

        private static bool Includes(Type type, IEnumerable<Type> includeTypes)
        {
            return includeTypes == null || includeTypes.Contains(type);
        }

        private static bool Excludes(Type type, IEnumerable<Type> excludeTypes)
        {
            return excludeTypes != null && excludeTypes.Contains(type);
        }
    }
}