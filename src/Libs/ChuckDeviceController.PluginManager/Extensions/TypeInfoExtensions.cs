namespace ChuckDeviceController.PluginManager.Extensions
{
    using System.Reflection;

    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Plugin.Services;

    public static class TypeInfoExtensions
    {
        private static readonly ILogger _logger =
            LoggerFactory.Create(x => x.AddConsole()).CreateLogger(nameof(TypeInfoExtensions));

        public static IEnumerable<Type> GetAssignableTypes<T>(this IEnumerable<Type> assemblyTypes)
        {
            var types = assemblyTypes.Where(type => typeof(T).IsAssignableFrom(type))
                                     .Where(type => (type.IsClass && !type.IsAbstract) || type.IsSubclassOf(typeof(T)))
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

        public static bool TrySetField<T>(this object instance, FieldInfo field, T fieldInstance)
        {
            try
            {
                field.SetValue(instance, fieldInstance);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, nameof(TrySetField));
            }
            return false;
        }

        public static bool TrySetProperty<T>(this object instance, PropertyInfo property, T propertyInstance)
        {
            try
            {
                property.SetValue(instance, propertyInstance);
                return true;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, nameof(TrySetProperty));
            }
            return false;
        }

        public static void SetPluginServiceFields(this object instance, IReadOnlyDictionary<Type, object> sharedServices)
        {
            instance.GetType()
                    .GetTypeInfo()
                    .SetPluginServiceFields(instance, sharedServices);
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
                    _logger.LogError($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for field '{fieldInfo.Name}' was null, skipping.");
                    continue;
                }

                if (sharedServices == null)
                {
                    _logger.LogError($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for field '{fieldInfo.Name}' was found, but shared host services is null, skipping.");
                    continue;
                }

                // Instantiate/set field to service instance
                if (!sharedServices.ContainsKey(attr.ServiceType))
                {
                    _logger.LogError($"Unable to find registered service '{attr.ServiceType.Name}' for plugin field '{fieldInfo.Name}' with attribute '{nameof(PluginBootstrapperServiceAttribute)}'");
                    continue;
                }

                var serviceHost = sharedServices[attr.ServiceType];
                instance.TrySetField(fieldInfo, serviceHost);
            }
        }

        public static void SetPluginServiceProperties(this object instance, IReadOnlyDictionary<Type, object> sharedServices)
        {
            instance.GetType()
                    .GetTypeInfo()
                    .SetPluginServiceProperties(instance, sharedServices);
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
                    _logger.LogError($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for property '{propertyInfo.Name}' was null, skipping.");
                    continue;
                }

                if (sharedServices == null)
                {
                    _logger.LogError($"Attribute '{nameof(PluginBootstrapperServiceAttribute)}' for property '{propertyInfo.Name}' was found, but shared host services is null, skipping.");
                    continue;
                }

                // Instantiate/set property to service
                if (!sharedServices.ContainsKey(attr.ServiceType))
                {
                    _logger.LogError($"Unable to find registered service '{attr.ServiceType.Name}' for plugin property '{propertyInfo.Name}' with attribute '{nameof(PluginBootstrapperServiceAttribute)}'");
                    continue;
                }

                var serviceHost = sharedServices[attr.ServiceType];
                instance.TrySetProperty(propertyInfo, serviceHost);
            }
        }

    }
}