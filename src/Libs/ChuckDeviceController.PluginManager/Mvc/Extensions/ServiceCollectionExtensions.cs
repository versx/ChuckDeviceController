namespace ChuckDeviceController.PluginManager.Mvc.Extensions;

using System.Reflection;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

using ChuckDeviceController.Plugin;
using ChuckDeviceController.Plugin.Services;
using ChuckDeviceController.PluginManager.FileProviders;
using ChuckDeviceController.PluginManager.Services;

public static class ServiceCollectionExtensions
{
    private const string DefaultPages = "Pages";
    private const string DefaultViews = "Views";
    private const string DefaultWebRoot = "wwwroot";

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

    public static (IFileProvider? Views, IFileProvider? WebRoot, IFileProvider? Pages) GetStaticFilesProvider(this Type pluginType, Assembly assembly)
    {
        // Check if plugin assembly has static files attribute assigned, if so add any embedded resource
        // or external files to web root provider i.e. 'wwwwroot' folder and contents
        var staticFilesAttr = pluginType.GetCustomAttribute<StaticFilesLocationAttribute>();
        if (staticFilesAttr == null)
            return default;

        var viewsFileProvider = staticFilesAttr.Views.GetFileProviderTypeFromStaticFileProvider(assembly, DefaultViews);
        var webRootFileProvider = staticFilesAttr.WebRoot.GetFileProviderTypeFromStaticFileProvider(assembly, DefaultWebRoot);
        var pagesFileProvider = staticFilesAttr.WebRoot.GetFileProviderTypeFromStaticFileProvider(assembly, DefaultPages);
        var result = (viewsFileProvider, webRootFileProvider, pagesFileProvider);
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

        if (staticFilesProvider.Pages != null)
        {
            env.WebRootFileProvider = new CompositeFileProvider(env.WebRootFileProvider, staticFilesProvider.Pages);
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
                var assemblyFolder = Path.GetDirectoryName(assembly.Location)!;
                var fullPath = Path.Combine(assemblyFolder, path);
                if (!Directory.Exists(fullPath))
                {
                    //var pluginName = assembly.GetName().Name;
                    //Console.WriteLine($"Error: External static directory '{fullPath}' for plugin '{pluginName}' does not exist");
                    return null;
                }
                // Static files are external and on local disk in the plugin's folder
                fileProvider = new PluginPhysicalFileProvider(assembly, fullPath);
                break;
        }
        return fileProvider;
    }

    public static object? CreatePluginInstance(this Type pluginType, IServiceProvider serviceProvider, IReadOnlyDictionary<Type, object> pluginServices)
    {
        var ctors = pluginType.GetPluginConstructors();
        if (!(ctors?.Any() ?? false))
        {
            // No matching constructors found
            return null;
        }

        var constructorInfo = ctors.First();
        var parameters = constructorInfo.GetParameters();
        var list = new List<object>(parameters.Length);
        var services = pluginType.BuildConstructorArgs(serviceProvider, pluginServices);

        // Loop the sonstructor's parameters to see which host type handlers
        // to provide it when we instantiate a new instance.
        foreach (var param in parameters)
        {
            if (!services.ContainsKey(param.ParameterType))
                continue;

            var service = services[param.ParameterType];
            list.Add(service);
        }

        var args = list.ToArray();
        var instance = pluginType.CreateInstance(args);
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
            var serviceDescriptor = new ServiceDescriptor(serviceType, implementation!, serviceLifetime);
            services.Add(serviceDescriptor);
        }
        return services;
    }

    public static string? GetPluginApiKey(this Type pluginType)
    {
        var attr = pluginType.GetCustomAttribute<PluginApiKeyAttribute>();
        return attr?.ApiKey;
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

    public static Dictionary<Type, object> BuildConstructorArgs(
        this Type type,
        IServiceProvider serviceProvider,
        IReadOnlyDictionary<Type, object> pluginServices)
    {
        var dict = new Dictionary<Type, object>(pluginServices);
        var serviceCollector = new ServiceParametersCollector(serviceProvider, pluginServices.Keys);
        var services = serviceCollector.GetParameterInstances(type);
        if (services.Any())
        {
            // Add all services to available services that can be injected to job controller
            foreach (var (serviceType, service) in services)
            {
                dict.Add(serviceType, service);
            }
        }
        return dict;
    }

    public static object? CreateInstance(this Type type, object[]? args = null)
    {
        try
        {
            object? instance;
            if (args?.Any() ?? false)
            {
                instance = Activator.CreateInstance(type, args);
            }
            else
            {
                instance = Activator.CreateInstance(type);
            }
            return instance;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            return null;
        }
    }
}