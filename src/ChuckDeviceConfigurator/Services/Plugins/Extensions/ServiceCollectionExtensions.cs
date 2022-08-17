namespace ChuckDeviceConfigurator.Services.Plugins.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc.ApplicationParts;

    using ChuckDeviceController.Plugins;
    using ChuckDeviceController.Plugins.Services;

    public static class ServiceCollectionExtensions
    {
        public static List<Type> GetAssignableTypes<T>(this IEnumerable<Type> assemblyTypes)
        {
            // TODO: Add type.IsSubClassOf support
            var types = assemblyTypes.Where(type => typeof(T).IsAssignableFrom(type))
                                     .Where(type => type.IsClass && !type.IsAbstract)
                                     .ToList();
            return types;
        }

        public static IEnumerable<(FieldInfo, T?)> GetFieldsOfCustomAttribute<T>(this TypeInfo type)
            where T : Attribute
        {
            return type.DeclaredFields
                       .Where(f => f.CustomAttributes.Any(c => c.AttributeType.Name == typeof(T).Name))
                       .Select(f => (f, f.GetCustomAttribute<T>()));
        }

        public static IEnumerable<(PropertyInfo, T?)> GetPropertiesOfCustomAttribute<T>(this TypeInfo type)
            where T : Attribute
        {
            return type.DeclaredProperties
                       .Where(p => p.CustomAttributes.Any(c => c.AttributeType.Name == typeof(T).Name))
                       .Select(p => (p, p.GetCustomAttribute<T>()));
        }

        public static bool TrySetField<T>(this object remoteInstance, FieldInfo field, T fieldInstance)
        {
            try
            {
                field.SetValue(remoteInstance, fieldInstance);
                return true;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            return false;
        }

        public static bool TrySetProperty<T>(this object remoteInstance, PropertyInfo property, T propertyInstance)
        {
            try
            {
                property.SetValue(remoteInstance, propertyInstance);
                return true;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            return false;
        }

        public static IServiceCollection RegisterPluginServiceWithAttribute(this IServiceCollection services, Type type)
        {
            var pluginService = type.GetPluginServiceWithAttribute();
            if (pluginService != null)
            {
                services.Add(pluginService);
            }
            return services;
        }

        public static ServiceDescriptor GetPluginServiceWithAttribute(this Type type)
        {
            var attr = type.GetCustomAttribute<PluginServiceAttribute>();
            if (attr == null)
                return null;

            var serviceType = attr.ServiceType;
            var implementation = attr.Provider == PluginServiceProvider.Plugin
                ? attr.ProxyType
                : type; // TODO: Get host service implementation
            var serviceLifetime = attr.Lifetime;
            var serviceDescriptor = new ServiceDescriptor(serviceType, implementation, serviceLifetime);
            return serviceDescriptor;
        }

        public static void SetPluginServiceFields(this TypeInfo typeInfo, object instance, IReadOnlyDictionary<Type, object> sharedServices)
        {
            var fields = typeInfo.GetFieldsOfCustomAttribute<PluginBootstrapperServiceAttribute>();
            if (!(fields?.Any() ?? false))
                return;

            foreach (var (fieldInfo, attr) in fields)
            {
                if (attr == null)
                {
                    Console.WriteLine($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for field '{fieldInfo.Name}' was null, skipping.");
                    continue;
                }

                if (sharedServices == null)
                {
                    Console.WriteLine($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for field '{fieldInfo.Name}' was found, but shared host services is null, skipping.");
                    continue;
                }

                // Instantiate/set field to service
                if (!sharedServices.ContainsKey(attr.ServiceType))
                {
                    Console.WriteLine($"Unable to find registered service '{attr.ServiceType.Name}' for plugin field '{fieldInfo.Name}' with attribute '{nameof(PluginBootstrapperServiceAttribute)}'");
                    continue;
                }

                var serviceHost = sharedServices[attr.ServiceType];
                instance.TrySetField(fieldInfo, serviceHost);
            }
        }
        
        public static void SetPluginServiceProperties(this TypeInfo typeInfo, object instance, IReadOnlyDictionary<Type, object> sharedServices)
        {
            var properties = typeInfo.GetPropertiesOfCustomAttribute<PluginBootstrapperServiceAttribute>();
            if (!(properties?.Any() ?? false))
                return;

            foreach (var (propertyInfo, attr) in properties)
            {
                if (attr == null)
                {
                    Console.WriteLine($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for property '{propertyInfo.Name}' was null, skipping.");
                    continue;
                }

                if (sharedServices == null)
                {
                    Console.WriteLine($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for property '{propertyInfo.Name}' was found, but shared host services is null, skipping.");
                    continue;
                }

                // Instantiate/set property to service
                if (!sharedServices.ContainsKey(attr.ServiceType))
                {
                    Console.WriteLine($"Unable to find registered service '{attr.ServiceType.Name}' for plugin property '{propertyInfo.Name}' with attribute '{nameof(PluginBootstrapperServiceAttribute)}'");
                    continue;
                }

                var serviceHost = sharedServices[attr.ServiceType];
                instance.TrySetProperty(propertyInfo, serviceHost);
            }
        }

        public static object CreatePluginInstance(this Type pluginType, IReadOnlyDictionary<Type, object> sharedServices)
        {
            object? instance;
            if (sharedServices == null)
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

        public static async Task<IServiceCollection> LoadPluginsAsync(this IServiceCollection services, IPluginManager pluginManager)
        {
            var finderOptions = new PluginFinderOptions
            {
                PluginType = typeof(IPlugin),
                RootPluginsDirectory = pluginManager.Options.RootPluginDirectory,
                ValidFileTypes = new[] { PluginFinderOptions.DefaultPluginFileType },
            };
            var pluginFinder = new PluginFinder<IPlugin>(finderOptions);
            var pluginFinderResults = pluginFinder.FindAssemliesWithPlugins();

            //services.AddControllersWithViews();
            var mvcBuilder = services.AddControllersWithViews();
            foreach (var pluginResult in pluginFinderResults)
            {
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

        /*
        public static async Task<IServiceCollection> LoadPluginsAsync(this IServiceCollection services, IPluginManager pluginManager)
        {
            var finderOptions = new PluginFinderOptions
            {
                PluginType = typeof(IPlugin),
                RootPluginsDirectory = pluginManager.Options.RootPluginDirectory,
                ValidFileTypes = new[] { ".dll" },
            };
            var pluginFinder = new PluginFinder<IPlugin>(finderOptions);
            var pluginFinderResults = pluginFinder.FindAssemliesWithPlugins();

            var mvcBuilder = services.AddControllers();
            foreach (var pluginResult in pluginFinderResults)
            {
                if (pluginResult.Assembly == null)
                {
                    Console.WriteLine($"Failed to load assembly for plugin '{pluginResult.FullAssemblyPath}', skipping.");
                    continue;
                }

                // Load plugin assembly as AssemblyPart for Mvc controllers
                var part = new AssemblyPart(pluginResult.Assembly);
                // Add loaded plugin assembly as application part
                mvcBuilder.PartManager.ApplicationParts.Add(part);

                //services.AddControllers().PartManager.ApplicationParts.Add(part);
                // TODO: Make single LoadPlugin(IServiceCollection services, Type type, etc) extension;

                var assemblyTypes = pluginResult.Assembly.GetTypes();
                var pluginBootstrapTypes = assemblyTypes.GetAssignableTypes<IPluginBootstrapper>();
                var pluginTypes = assemblyTypes.GetAssignableTypes<IPlugin>();

                // Register all marked service classes with 'PluginServiceAttribute'
                foreach (var type in assemblyTypes) 
                {
                    // Register services with 'PluginServiceAttribute'
                    services.RegisterPluginServiceWithAttribute(type);
                }

                // Loop all found plugin types and create/instantiate instances of them
                foreach (var pluginType in pluginTypes)
                {
                    // Instantiate an instance of the plugin type
                    var pluginInstance = pluginType.CreatePluginInstance(pluginManager.Options.SharedServiceHosts);
                    if (pluginInstance == null)
                    {
                        Console.WriteLine($"Failed to instantiate plugin type instance '{pluginType.Name}'");
                        continue;
                    }

                    if (pluginInstance is not IPlugin plugin)
                    {
                        Console.WriteLine($"Failed to instantiate '{nameof(IPlugin)}' instance");
                        continue;
                    }

                    // Initialize any fields or properties marked as plugin service types
                    var typeInfo = pluginInstance.GetType().GetTypeInfo();
                    typeInfo.SetPluginServiceFields(pluginInstance, pluginManager.Options.SharedServiceHosts);
                    typeInfo.SetPluginServiceProperties(pluginInstance, pluginManager.Options.SharedServiceHosts);

                    var permissions = pluginType.GetPermissions();
                    var pluginHost = new PluginHost(
                        plugin,
                        permissions,
                        pluginResult,
                        new PluginEventHandlers(),
                        PluginState.Running
                    );

                    foreach (var type in pluginType.GetInterfaces())
                    {
                        if (typeof(IUiEvents) == type)
                            pluginHost.EventHandlers.UiEvents = (IUiEvents)plugin;
                        else if (typeof(IDatabaseEvents) == type)
                            pluginHost.EventHandlers.DatabaseEvents = (IDatabaseEvents)plugin;
                        else if (typeof(IJobControllerServiceHost) == type)
                            pluginHost.EventHandlers.JobControllerEvents = (IJobControllerServiceEvents)plugin;
                    }

                    // Call plugin's ConfigureServices method to register any services
                    plugin.ConfigureServices(services);

                    // Call plugin's load method
                    plugin.OnLoad();

                    // Register plugin with plugin manager
                    await pluginManager.RegisterPluginAsync(pluginHost);
                }
            }
            return services;
        }
        */

        public static PluginPermissions GetPermissions(this Type pluginType)
        {
            var attributes = pluginType.GetCustomAttributes<PluginPermissionsAttribute>();
            if (attributes.Any())
            {
                var attr = attributes.FirstOrDefault();
                return attr?.Permissions ?? PluginPermissions.None;
            }
            return PluginPermissions.None;
        }

        public static object[] GetConstructorArgs(this Type pluginType, IReadOnlyDictionary<Type, object>? sharedHosts = null)
        {
            var constructors = pluginType.GetConstructors();
            if ((constructors?.Length ?? 0) == 0)
            {
                Console.WriteLine($"Plugins must only contain one constructor for each class that inherits from '{nameof(IPlugin)}', skipping registration for plugin '{pluginType.Name}'");
                return null;
            }

            var constructorInfo = constructors![0];
            var parameters = constructorInfo.GetParameters();
            var list = new List<object>(parameters.Length);

            // Check that we were provided shared host types
            if ((sharedHosts?.Count ?? 0) > 0)
            {
                // Loop the plugin's constructor parameters to see which host type handlers
                // to provide it when we instantiate a new instance.
                foreach (var param in parameters)
                {
                    if (!sharedHosts!.ContainsKey(param.ParameterType))
                        continue;

                    var pluginHostHandler = sharedHosts[param.ParameterType];
                    list.Add(pluginHostHandler);
                }
            }
            return list.ToArray();
        }
    }
}