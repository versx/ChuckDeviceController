namespace ChuckDeviceConfigurator.Extensions;

using System.Reflection;

using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.PluginManager;
using ChuckDeviceController.PluginManager.Mvc.Extensions;

public static class TypeExtensions
{
    public static object[]? BuildJobControllerConstructorArgs(
        this Type jobControllerType,
        IInstance instance,
        IReadOnlyList<Geofence> geofences,
        IReadOnlyDictionary<Type, object> sharedServices,
        ServiceProvider serviceProvider)
    {
        // Construct dictionary of plugin specific service parameters
        var dict = jobControllerType.GetPluginServiceParameters(instance, geofences, sharedServices);

        // Get services not specific to plugins or job controllers while ignoring any plugin specific parameter types
        var services = jobControllerType.BuildConstructorArgs(serviceProvider, dict);

        var ctors = jobControllerType.GetPluginConstructors();
        if (!(ctors?.Any() ?? false))
        {
            // No matching constructors found
            return null;
        }

        var constructorInfo = ctors.First();
        var parameters = constructorInfo.GetParameters();
        var list = new List<object>(parameters.Length);

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
        return args;
    }

    public static Dictionary<Type, object> GetPluginServiceParameters(
        this Type jobControllerType,
        IInstance instance,
        IReadOnlyList<Geofence> geofences,
        IReadOnlyDictionary<Type, object> sharedServices)
    {
        var dict = new Dictionary<Type, object>(sharedServices)
        {
            { typeof(IInstance), instance },
            { typeof(IReadOnlyList<IGeofence>), geofences }
        };

        var attr = jobControllerType.GetCustomAttribute<GeofenceTypeAttribute>(false);
        if (attr == null)
        {
            // Failed to find 'GeofenceTypeAttribute' for job controller
            // Return dictionary of plugin services
            return dict;
        }

        switch (attr?.Type)
        {
            case GeofenceType.Circle:
                // Add list of coordinates to available parameters list
                var circles = geofences.ConvertToCoordinates();
                dict.Add(typeof(List<ICoordinate>), circles);
                break;
            case GeofenceType.Geofence:
                // Add list of geofence coordinates to available parameters list
                var (multiPolygons, polyCoords) = geofences.ConvertToMultiPolygons();
                var coords = polyCoords
                    .Select(c => c.Select(coord => (ICoordinate)coord).ToList())
                    .ToList();
                dict.Add(typeof(List<List<ICoordinate>>), coords);
                dict.Add(typeof(List<IMultiPolygon>), multiPolygons);
                break;
        }

        return dict;
    }

    /// <summary>
    /// Retrieves the non instantiated classes which have attribute T, or if any of
    /// the methods or properties have attribute T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>List&lt;Type&gt;</returns>
    public static IEnumerable<Type> GetTypes<T>(IEnumerable<IPluginHost> plugins)
    {
        var results = new List<Type>();
        foreach (var plugin in plugins)
        {
            try
            {
                foreach (var type in plugin.Assembly.Types)
                {
                    try
                    {
                        if (type.HasAttributeType<T>())
                        {
                            results.Add(type);
                            continue;
                        }

                        var types = type.GetTypes<T>();
                        if (types?.Any() ?? false)
                        {
                            results.AddRange(types);
                        }
                    }
                    catch (Exception typeLoader)
                    {
                        Console.WriteLine($"[{plugin.Plugin.Name}] {MethodBase.GetCurrentMethod()?.Name} {typeLoader}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{plugin.Plugin.Name}] {MethodBase.GetCurrentMethod()?.Name}: {ex}");
            }
        }

        return results;
    }

    public static IEnumerable<Type> GetTypes<T>(this Type type)
    {
        var results = new List<Type>();
        if (type.IsClass || type.IsInterface)
        {
            // Cycle through all properties and methods to see if they have the attibute
            var methodTypes = type.GetMethodTypes<T>();
            if (methodTypes?.Any() ?? false)
            {
                results.AddRange(methodTypes);
            }

            var propertyTypes = type.GetPropertyTypes<T>();
            if (propertyTypes?.Any() ?? false)
            {
                results.AddRange(propertyTypes);
            }
        }
        return results;
    }

    public static IEnumerable<Type> GetMethodTypes<T>(this Type type)
    {
        foreach (var method in type.GetMethods())
        {
            if (method.HasAttributeType<T>())
            {
                yield return type;
            }
        }
    }

    public static IEnumerable<Type> GetPropertyTypes<T>(this Type type)
    {
        foreach (var property in type.GetProperties())
        {
            if (property.HasAttributeType<T>())
            {
                yield return type;
            }
        }
    }

    public static bool HasAttributeType<T>(this MemberInfo member)
    {
        var result = member.GetCustomAttributes()
                           .Where(t => t.GetType() == typeof(T))
                           .FirstOrDefault() != null;
        return result;
    }
}