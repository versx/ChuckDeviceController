namespace ChuckDeviceConfigurator.Extensions
{
    using System.Reflection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Geometry.Models.Contracts;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.PluginManager;
    using ChuckDeviceController.PluginManager.Mvc.Extensions;

    public static class TypeExtensions
    {
        public static object[]? GetJobControllerConstructorArgs(
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

            var attributes = jobControllerType.GetCustomAttributes(typeof(GeofenceTypeAttribute), false);
            if (!(attributes?.Any() ?? false))
            {
                // No geofence attributes specified but is required
                return null;
            }

            var ctors = jobControllerType.GetPluginConstructors();
            if (!(ctors?.Any() ?? false))
            {
                // No matching constructors found
                return null;
            }

            var constructorInfo = ctors.First();
            var parameters = constructorInfo.GetParameters();
            var list = new List<object>(parameters.Length);

            // TODO: Remove GeofenceTypeAttribute requirement
            if (attributes!.FirstOrDefault() is not GeofenceTypeAttribute attr)
            {
                // Failed to find 'GeofenceTypeAttribute' for job controller
                return null;
            }

            switch (attr?.Type)
            {
                case GeofenceType.Circle:
                    var circles = geofences.ConvertToCoordinates();
                    dict.Add(typeof(List<ICoordinate>), circles);
                    break;
                case GeofenceType.Geofence:
                    var (multiPolygons, polyCoords) = geofences.ConvertToMultiPolygons();
                    var coords = polyCoords
                        .Select(c => c.Select(coord => (ICoordinate)coord).ToList())
                        .ToList();
                    dict.Add(typeof(List<List<ICoordinate>>), coords);
                    dict.Add(typeof(List<IMultiPolygon>), multiPolygons);
                    break;
            }

            // Loop the sonstructor's parameters to see which host type handlers
            // to provide it when we instantiate a new instance.
            foreach (var param in parameters)
            {
                if (!dict.ContainsKey(param.ParameterType))
                    continue;

                var hostHandler = dict[param.ParameterType];
                list.Add(hostHandler);
            }

            var args = list.ToArray();
            return args;
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
}