namespace ChuckDeviceConfigurator.Services.Plugins.Extensions
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc.ApplicationParts;

    using ChuckDeviceController.Plugins;
    using ChuckDeviceController.Plugins.Services;

    public class PluginOptions
    {
    }

    public static class ServiceCollectionExtensions
    {
        public static List<Type> GetAssignableTypes<T>(this IEnumerable<Type> assemblyTypes)
        {
            var types = assemblyTypes.Where(type => typeof(T).IsAssignableFrom(type))
                                     .Where(type => type.IsClass && !type.IsAbstract)
                                     .ToList();
            return types;
        }

        public static IEnumerable<(FieldInfo, T)> GetFieldsOfCustomAttribute<T>(this TypeInfo type)
            where T : Attribute
        {
            return type.DeclaredFields
                       .Where(f => f.CustomAttributes.Any(c => c.AttributeType.Name == typeof(T).Name))
                       .Select(f => (f, f.GetCustomAttribute<T>()));
        }

        public static IEnumerable<(PropertyInfo, T)> GetPropertiessOfCustomAttribute<T>(this TypeInfo type)
            where T : Attribute
        {
            return type.DeclaredProperties
                       .Where(p => p.CustomAttributes.Any(c => c.AttributeType.Name == typeof(T).Name))
                       .Select(p => (p, p.GetCustomAttribute<T>()));
        }

        public static bool TrySetField<T>(this object remoteInstance, string fieldName, T fieldInstance)
        {
            return TrySetField(remoteInstance.GetType(), fieldName, fieldInstance);
        }

        public static bool TrySetField<T>(this Type remoteInstance, string fieldName, T fieldInstance)
        {
            return TrySetField(remoteInstance.GetTypeInfo(), fieldName, fieldInstance);
        }

        public static bool TrySetField<T>(this TypeInfo remoteInstance, string fieldName, T fieldInstance)
        {
            try
            {
                //remoteInstance.DeclaredFields
                //              .First(f => f.Name == fieldName)
                //              .SetValue(remoteInstance, fieldInstance);
                var field = remoteInstance.DeclaredFields
                              .First(f => f.Name == fieldName);
                field.SetValue(remoteInstance, fieldInstance);
                return true;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
            return false;
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

        public static IServiceCollection LoadPlugins(this IServiceCollection services, string pluginFolderPath, IReadOnlyDictionary<Type, object>? sharedHosts = null, Action<PluginOptions>? configure = null)
        {
            var pluginFinder = new PluginFinder<IPlugin>(pluginFolderPath);
            var pluginFilePaths = pluginFinder.FindAssemliesWithPlugins();

            foreach (var pluginFile in pluginFilePaths)
            {
                var assembly = Assembly.LoadFrom(pluginFile);
                var part = new AssemblyPart(assembly);
                services.AddControllers().PartManager.ApplicationParts.Add(part);

                var assemblyTypes = assembly.GetTypes();
                var pluginBootstrapTypes = assemblyTypes.GetAssignableTypes<IPluginBootstrapper>();
                var pluginTypes = assemblyTypes.GetAssignableTypes<IPlugin>();

                foreach (var type in assemblyTypes) //pluginTypes
                {
                    // TODO: Keep track of plugins

                    // Register services with 'PluginServiceAttribute'
                    var pluginServiceAttr = type.GetCustomAttribute<PluginServiceAttribute>();
                    if (pluginServiceAttr != null)
                    {
                        var serviceType = pluginServiceAttr.ServiceType;
                        var implementation = pluginServiceAttr.ProxyType;
                        var serviceLifetime = pluginServiceAttr.Lifetime;
                        var serviceDescriptor = new ServiceDescriptor(serviceType, type, serviceLifetime);
                        services.Add(serviceDescriptor);
                    }

                    if (!(typeof(IPlugin).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract))
                        continue;

                    object? instance;
                    if (sharedHosts == null)
                    {
                        instance = Activator.CreateInstance(type);
                    }
                    else
                    {
                        var diParams = GetDiParameters(type, sharedHosts);
                        instance = Activator.CreateInstance(type, diParams);
                    }

                    if (instance == null)
                    {
                        Console.WriteLine($"Failed to instantiate plugin type instance '{type.Name}'");
                        continue;
                    }

                    var typeInfo = instance.GetType().GetTypeInfo();
                    var fields = typeInfo.GetFieldsOfCustomAttribute<PluginBootstrapperServiceAttribute>();
                    var properties = typeInfo.GetPropertiessOfCustomAttribute<PluginBootstrapperServiceAttribute>();
                    if (fields?.Any() ?? false)
                    {
                        foreach (var (fieldInfo, attr) in fields)
                        {
                            // Instantiate/set field to service
                            var serviceHost = sharedHosts[attr.ServiceType];
                            instance.TrySetField(fieldInfo, serviceHost);

                        }
                    }
                    if (properties?.Any() ?? false)
                    {
                        foreach (var (propertyInfo, attr) in properties)
                        {
                            // TODO: Instantiate/set property to service
                        }
                    }

                    var plugin = instance as IPlugin;
                    if (plugin == null)
                    {
                        Console.WriteLine($"Failed to instantiate '{nameof(IPlugin)}' instance");
                        continue;
                    }
                    plugin.ConfigureServices(services);
                    plugin.OnLoad();
                }
            }
            return services;
        }

        public static object[] GetDiParameters(Type pluginType, IReadOnlyDictionary<Type, object>? sharedHosts = null)
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