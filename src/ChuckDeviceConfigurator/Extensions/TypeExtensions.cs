namespace ChuckDeviceConfigurator.Extensions
{
    using System.Reflection;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Plugin;

    public static class TypeExtensions
    {
        public static object[]? GetJobControllerConstructorArgs(this Type jobControllerType, IInstance instance, IReadOnlyList<Geofence> geofences)
        {
            var attributes = jobControllerType.GetCustomAttributes(typeof(GeofenceTypeAttribute), false);
            if (!(attributes?.Any() ?? false))
            {
                // No geofence attributes specified but is required
                return null;
            }

            object[]? args = null;
            var attr = attributes!.FirstOrDefault() as GeofenceTypeAttribute;

            switch (attr?.Type)
            {
                case GeofenceType.Circle:
                    var circles = geofences.ConvertToCoordinates();
                    args = new object[] { instance, circles };
                    break;
                case GeofenceType.Geofence:
                    var (_, polyCoords) = geofences.ConvertToMultiPolygons();
                    args = new object[] { instance, polyCoords };
                    break;
            }
            return args;
        }

        /// <summary>
        /// Retrieves the non instantiated classes which have attribute T, or if any of
        /// the methods or properties have attribute T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>List&lt;Type&gt;</returns>
        public static IEnumerable<Type> GetTypes<T>(IEnumerable<ChuckDeviceController.PluginManager.IPluginHost> plugins)
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