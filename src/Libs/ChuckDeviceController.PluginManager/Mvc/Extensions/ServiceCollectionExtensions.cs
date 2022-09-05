namespace ChuckDeviceController.PluginManager.Mvc.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;

    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.Services;
    using ChuckDeviceController.PluginManager.FileProviders;

    public static class ServiceCollectionExtensions
    {
        private const string DefaultViews = "Views";
        private const string DefaultWebRoot = "wwwroot";

        public static T GetService<T>(this IServiceCollection services) where T : notnull
        {
            var scopeFactory = services.BuildServiceProvider()
                                       .GetRequiredService<IServiceScopeFactory>();

            using var scope = scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;
            var service = provider.GetRequiredService<T>();
            return service;
        }

        public static IServiceCollection RegisterPluginServices(this IServiceCollection services, IEnumerable<ServiceDescriptor> pluginServices)
        {
            // Register any PluginServices found with IServiceCollection
            if (!pluginServices.Any())
                return services;

            foreach (var pluginService in pluginServices)
            {
                // Register service found in plugin assembly
                services.Add(pluginService);
            }
            return services;
        }

        public static (IFileProvider? Views, IFileProvider? WebRoot) GetStaticFilesProvider(this Type pluginType, Assembly assembly)
        {
            // Check if plugin assembly has static files attribute assigned, if so add any embedded resource
            // or external files to web root provider i.e. 'wwwwroot' folder and contents
            var staticFilesAttr = pluginType.GetCustomAttribute<StaticFilesLocationAttribute>();
            if (staticFilesAttr == null)
                return default;

            var viewsFileProvider = staticFilesAttr.Views.GetFileProviderTypeFromStaticFileProvider(assembly, DefaultViews);
            var webRootFileProvider = staticFilesAttr.WebRoot.GetFileProviderTypeFromStaticFileProvider(assembly, DefaultWebRoot);
            var result = (viewsFileProvider, webRootFileProvider);
            return result;
        }

        public static void RegisterStaticFiles(this Type pluginType, IWebHostEnvironment env, Assembly assembly)
        {
            var staticFilesProvider = pluginType.GetStaticFilesProvider(assembly);
            if (staticFilesProvider == default)
                return;

            if (staticFilesProvider.Views != null)
            {
                env.WebRootFileProvider = new CompositeFileProvider(env.WebRootFileProvider, staticFilesProvider.Views);
            }

            // Register a new composite file provider containing the old 'wwwroot' file provider
            // and our new one. Adding another web root file provider needs to be done before
            // the call to 'app.UseStaticFiles'
            if (staticFilesProvider.WebRoot != null)
            {
                env.WebRootFileProvider = new CompositeFileProvider(env.WebRootFileProvider, staticFilesProvider.WebRoot);
            }
        }

        public static IFileProvider? GetFileProviderTypeFromStaticFileProvider(this StaticFilesLocation location, Assembly assembly, string path)
        {
            IFileProvider? fileProvider = null;
            switch (location)
            {
                case StaticFilesLocation.None:
                    // No static files to map/worry about
                    return null;
                case StaticFilesLocation.Resources:
                    // Check that we have any embedded resources in the plugin
                    var resourceNames = assembly.GetManifestResourceNames();
                    if (!(resourceNames?.Any() ?? false))
                        return null;

                    // Static files are within the plugin's embedded resources file
                    fileProvider = new ManifestEmbeddedFileProvider(assembly, path);
                    break;
                case StaticFilesLocation.External:
                    if (!File.Exists(path))
                    {
                        Console.WriteLine($"Error: External static file '{path}' does not exist");
                        return null;
                    }
                    // Static files are external and on local disk in the plugin's folder
                    fileProvider = new PluginPhysicalFileProvider(assembly, path);
                    break;
            }
            return fileProvider;
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

        public static IEnumerable<ServiceDescriptor> GetPluginServicesWithAttribute(this Type type, IReadOnlyDictionary<Type, object> sharedServices)
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
                Type? implementation = null;
                switch (attr.Provider)
                {
                    case PluginServiceProvider.Host:
                        if (sharedServices.ContainsKey(serviceType))
                        {
                            implementation = sharedServices[serviceType].GetType();
                        }
                        break;
                    case PluginServiceProvider.Plugin:
                    default:
                        implementation = attr.ProxyType;
                        break;
                }
                var serviceLifetime = attr.Lifetime;
                var serviceDescriptor = new ServiceDescriptor(serviceType, implementation, serviceLifetime);
                services.Add(serviceDescriptor);
            }
            return services;
        }

        public static PluginPermissions GetPluginPermissions(this Type pluginType)
        {
            var attr = pluginType.GetCustomAttribute<PluginPermissionsAttribute>();
            return attr?.Permissions ?? PluginPermissions.None;
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

        #region Service Type Discovery/Instantiation
        // Credits: https://github.com/k3ldar/.NetCorePluginManager/blob/master/PluginManager/src/Helpers/ServiceCollectionHelper.cs

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

        /// <summary>
        /// Grab a list of all public constructors in the class, starting with the constructor
        /// with most parameters.
        /// </summary>
        /// <param name="type">Generic type to retrieve constructors from.</param>
        /// <returns>Returns a list of constructors found in the generic type.</returns>
        public static IEnumerable<ConstructorInfo> GetPluginConstructors(this Type type)
        {
            var constructors = type
                .GetConstructors()
                .Where(c => c.IsPublic && !c.IsStatic)// && c.GetParameters().Length > 0)
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
        public static T? GetServiceInstance<T>(this IServiceCollection services)
            where T : class
        {
            if (services == null)
            {
                return null;
            }

            return GetClassImplementation<T>(services, typeof(T));
        }

        private static T? GetClassImplementation<T>(IServiceCollection services, Type classType)
            where T : class
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
                var parameters = constructor.GetParameters();
                foreach (var param in parameters)
                {
                    //if (param.ParameterType == typeof(IServiceProvider))
                    //    continue;

                    var paramClass = GetClassImplementation<object>(services, param.ParameterType);
                    if (paramClass == null)
                    {
                        // If we did not find a specific param type for this constructor, try the next constructor
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

        #endregion
    }
}