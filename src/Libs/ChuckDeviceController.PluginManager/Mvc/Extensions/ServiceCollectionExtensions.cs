namespace ChuckDeviceController.PluginManager.Mvc.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.Extensions.DependencyInjection;

    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.Services;
    using ChuckDeviceController.PluginManager;
    using ChuckDeviceController.PluginManager.Mvc.Razor;
    using ChuckDeviceController.PluginManager.Services.Finder;
    using ChuckDeviceController.PluginManager.Services.Loader;

    public static class ServiceCollectionExtensions
    {
        public static async Task<IServiceCollection> LoadPluginsAsync(this IServiceCollection services, IPluginManager pluginManager, string contentRootPath)
        {
            var finderOptions = new PluginFinderOptions
            {
                PluginType = typeof(IPlugin),
                RootPluginsDirectory = pluginManager.Options.RootPluginDirectory,
                ValidFileTypes = new[] { PluginFinderOptions.DefaultPluginFileType },
            };
            var pluginFinder = new PluginFinder<IPlugin>(finderOptions);
            var pluginFinderResults = pluginFinder.FindAssemliesWithPlugins();

            if (!(pluginFinderResults?.Any() ?? false))
            {
                // Failed to find any eligible plugins to load
                return services;
            }

            // Replace Razor view compiler, faster than using
            // 'services.Configure<RazorViewEngineOptions>(options => options.ViewLocationExpanders)'
            services.Replace<IViewCompilerProvider, PluginViewCompilerProvider>();

            var mvcBuilder = services
                .AddControllersWithViews()
                .AddRazorRuntimeCompilation();

            foreach (var pluginResult in pluginFinderResults)
            {
                // TODO: New service collection for each plugin?
                // TODO: Register sharedServiceHosts with new service collection if so
                //var serviceCollection = new ServiceCollection();
                if (pluginResult.Assembly == null)
                {
                    Console.WriteLine($"Failed to load assembly for plugin '{pluginResult.FullAssemblyPath}', skipping.");
                    continue;
                }

                // Load and activate plugins found by plugin finder
                var pluginLoader = new PluginLoader<IPlugin>(pluginResult, pluginManager.Options.SharedServiceHosts);
                var loadedPlugins = pluginLoader.LoadedPlugins;
                if (!loadedPlugins.Any())
                {
                    Console.WriteLine($"Failed to find any valid plugins in assembly '{pluginResult.FullAssemblyPath}'");
                    continue;
                }

                // Register assembly as application part with Mvc
                mvcBuilder.AddApplicationPart(pluginResult.Assembly);

                // Register all available host services with plugin
                mvcBuilder.AddServices(services);

                // Loop through all loaded plugins and register plugin services and register plugins
                foreach (var pluginHost in loadedPlugins)
                {
                    // Register any PluginServices found with IServiceCollection
                    var pluginServices = pluginHost.PluginServices;
                    if (pluginServices.Any())
                    {
                        foreach (var pluginService in pluginServices)
                        {
                            // Register found plugin service
                            services.Add(pluginService);
                        }
                    }

                    // Call plugin's ConfigureServices method to register any services
                    pluginHost.Plugin.ConfigureServices(services);

                    // Call plugin's load method
                    pluginHost.Plugin.OnLoad();

                    // Register plugin host with plugin manager
                    await pluginManager.RegisterPluginAsync(pluginHost);
                }
            }
            return services;
        }

        public static object? CreatePluginInstance(this Type pluginType, IReadOnlyDictionary<Type, object>? sharedServices = null)
        {
            object? instance;
            if (!(sharedServices?.Any() ?? false))
            {
                instance = Activator.CreateInstance(pluginType);
            }
            else
            {
                var args = pluginType.GetConstructorArgs(sharedServices);
                instance = Activator.CreateInstance(pluginType, args);
            }
            return instance;
        }

        public static IEnumerable<ServiceDescriptor> GetPluginServicesWithAttribute(this Type type)
        {
            var services = new List<ServiceDescriptor>();
            var attributes = type.GetCustomAttributes<PluginServiceAttribute>();
            if (!(attributes?.Any() ?? false))
            {
                return services;
            }
            foreach (var attr in attributes)
            {
                var serviceType = attr.ServiceType;
                var implementation = attr.Provider == PluginServiceProvider.Plugin
                    ? attr.ProxyType
                    : type; // TODO: Get host service implementation
                var serviceLifetime = attr.Lifetime;
                var serviceDescriptor = new ServiceDescriptor(serviceType, implementation, serviceLifetime);
                services.Add(serviceDescriptor);
            }
            return services;
        }

        public static PluginPermissions GetPluginPermissions(this Type pluginType)
        {
            var attributes = pluginType.GetCustomAttributes<PluginPermissionsAttribute>();
            if (attributes.Any())
            {
                var attr = attributes.FirstOrDefault();
                return attr?.Permissions ?? PluginPermissions.None;
            }
            return PluginPermissions.None;
        }

        public static object[]? GetConstructorArgs(this Type pluginType, IReadOnlyDictionary<Type, object>? sharedServices = null)
        {
            var constructors = pluginType.GetPluginConstructors();
            if (!(constructors?.Any() ?? false))
            {
                Console.WriteLine($"Plugins must contain one constructor for each class that inherits from '{nameof(IPlugin)}', skipping registration for plugin type '{pluginType.Name}'");
                return null;
            }

            var constructorInfo = constructors.First();
            var parameters = constructorInfo.GetParameters();
            var list = new List<object>(parameters.Length);

            // Check that we were provided shared host types
            if (!(sharedServices?.Any() ?? false))
                return list.ToArray();

            // Loop the plugin's constructor parameters to see which host type handlers
            // to provide it when we instantiate a new instance.
            foreach (var param in parameters)
            {
                if (!sharedServices!.ContainsKey(param.ParameterType))
                    continue;

                var pluginHostHandler = sharedServices[param.ParameterType];
                list.Add(pluginHostHandler);
            }

            return list.ToArray();
        }

        #region Replace Services

        public static IServiceCollection Replace<TService, TImplementation>(this IServiceCollection services)
            where TImplementation : TService
        {
            return services.Replace<TService>(typeof(TImplementation));
        }

        public static IServiceCollection Replace<TService>(this IServiceCollection services, Type implementationType)
        {
            return services.Replace(typeof(TService), implementationType);
        }

        public static IServiceCollection Replace(this IServiceCollection services, Type serviceType, Type implementationType)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (implementationType == null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            if (!services.TryGetDescriptors(serviceType, out var descriptors))
            {
                throw new ArgumentException($"No services found for {serviceType.FullName}.", nameof(serviceType));
            }

            foreach (var descriptor in descriptors)
            {
                var index = services.IndexOf(descriptor);
                services.Insert(index, descriptor.WithImplementationType(implementationType));
                services.Remove(descriptor);
            }

            return services;
        }

        private static bool TryGetDescriptors(this IServiceCollection services, Type serviceType, out ICollection<ServiceDescriptor> descriptors)
        {
            return (descriptors = services.Where(service => service.ServiceType == serviceType).ToArray()).Any();
        }

        private static ServiceDescriptor WithImplementationType(this ServiceDescriptor descriptor, Type implementationType)
        {
            return new ServiceDescriptor(descriptor.ServiceType, implementationType, descriptor.Lifetime);
        }

        #endregion

        public static object[] GetParameterInstances(this IServiceCollection services, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (services != null)
            {
                return GetInstancesConstructorParameters(services, type);
            }

            var result = new List<object>();
            var constructors = type.GetPluginConstructors();
            foreach (var constructor in constructors)
            {
                foreach (var param in constructor.GetParameters())
                {
                    var paramClass = services?.FirstOrDefault(service => service.ServiceType == param.ParameterType);
                    if (paramClass == null)
                        continue;

                    // If we did not find a specific param type for this constructor, try the next constructor
                    if (paramClass == null)
                    {
                        result.Clear();
                        break;
                    }

                    result.Add(paramClass);
                }

                if (result.Count > 0)
                {
                    return result.ToArray();
                }
            }

            return result.ToArray();
        }

        public static IEnumerable<ConstructorInfo> GetPluginConstructors(this Type type)
        {
            // Grab a list of all constructors in the class, start with the one with most parameters
            var constructors = type
                .GetConstructors()
                .Where(c => c.IsPublic && !c.IsStatic && c.GetParameters().Length > 0)
                .OrderByDescending(c => c.GetParameters().Length)
                .ToList();
            return constructors;
        }

        /// <summary>
        /// Retrieves an instance of a class from within an IServiceCollection
        /// </summary>
        /// <typeparam name="T">Type of class instance being sought</typeparam>
        /// <param name="services"></param>
        /// <returns>Instance of type T if found within the service collection, otherwise null</returns>
        public static T? GetServiceInstance<T>(this IServiceCollection services) where T : class
        {
            if (services == null)
            {
                return null;
            }

            return GetClassImplementation<T>(services, typeof(T));
        }

        private static T? GetClassImplementation<T>(IServiceCollection services, Type classType) where T : class
        {
            var sd = services
                .Where(sd => GetNameWithoutGenericArity(sd.ServiceType).Equals(GetNameWithoutGenericArity(classType)))
                .FirstOrDefault();
            if (sd == null)
            {
                return default;
            }

            T? result = default;

            if (sd.ImplementationInstance != null)
            {
                result = (T)sd.ImplementationInstance;
            }
            else if (sd.ImplementationType != null)
            {
                var args = GetInstancesConstructorParameters(services, sd.ImplementationType);
                result = (T?)Activator.CreateInstance(sd.ImplementationType, args);

                if (sd.Lifetime == ServiceLifetime.Singleton)
                {
                    var replacementServiceDescriptor = new ServiceDescriptor(sd.ServiceType, result);

                    services.Remove(sd);
                    services.Add(replacementServiceDescriptor);
                }
            }
            else if (sd.ImplementationFactory != null)
            {
                result = sd.ImplementationFactory.Invoke(null) as T;
            }

            return result;
        }

        private static string GetNameWithoutGenericArity(Type type)
        {
            var name = type.FullName;
            var index = name?.IndexOf('`') ?? -1;
            return index == -1
                ? name!
                : name![..index];
        }

        private static object[] GetInstancesConstructorParameters(IServiceCollection services, Type type)
        {
            var result = new List<object>();
            var constructors = type.GetPluginConstructors();
            foreach (var constructor in constructors)
            {
                foreach (var param in constructor.GetParameters())
                {
                    var paramClass = GetClassImplementation<object>(services, param.ParameterType);

                    // If we did not find a specific param type for this constructor, try the next constructor
                    if (paramClass == null)
                    {
                        result.Clear();
                        break;
                    }

                    result.Add(paramClass);
                }

                if (result.Count > 0)
                {
                    return result.ToArray();
                }
            }

            return result.ToArray();
        }
    }
}