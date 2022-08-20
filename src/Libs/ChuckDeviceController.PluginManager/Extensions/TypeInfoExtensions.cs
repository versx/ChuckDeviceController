namespace ChuckDeviceController.PluginManager.Extensions
{
    using System.Reflection;

    using ChuckDeviceController.Plugin.Services;

    public static class TypeInfoExtensions
    {
        public static IEnumerable<Type> GetAssignableTypes<T>(this IEnumerable<Type> assemblyTypes)
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

    }
}