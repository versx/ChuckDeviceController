namespace ChuckDeviceController.Data.Extensions
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Reflection;

    using Team = POGOProtos.Rpc.Team;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Extensions.Json;

    public static class PropertyInfoExtensions
    {
        #region Constants

        private const string DbNull = "NULL";

        #endregion

        #region Variables

        private static readonly IReadOnlyDictionary<Type, Func<object, object>> _enumsToConvert = new Dictionary<Type, Func<object, object>>
        {
            { typeof(WeatherCondition), x => Convert.ToInt32(x) },
            { typeof(Team), x => Convert.ToInt32(x) },
        };
        private static readonly IEnumerable<Type> _typesToConvertJson = new[]
        {
            typeof(Dictionary<,>),
            typeof(List<>),
            typeof(IReadOnlyDictionary<,>),
            typeof(IReadOnlyList<>),
            typeof(IReadOnlyCollection<>),
            typeof(IDictionary<,>),
            typeof(IList<>),
            typeof(IEnumerable<>),
        };

        #endregion

        public static IEnumerable<string> GetPropertyNames<TEntity>(
            this TEntity entity,
            IEnumerable<string>? includedProperties = null,
            IEnumerable<string>? ignoredProperties = null)
        {
            // Include only public instance properties as well as base inherited properties
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var properties = entity!.GetType().GetProperties(flags);
            //var properties = TypeDescriptor.GetProperties(typeof(TEntity));

            foreach (var prop in properties)
            {
                // Ignore any properties specified that match the entity
                if (ignoredProperties?.Contains(prop.Name) ?? false ||
                    !(includedProperties?.Contains(prop.Name) ?? true))
                    continue;

                // Ignore any properties that are not mapped to a database table via an attribute
                // or if the property is explicitly set to not be mapped.
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr == null)// ||
                    //prop.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;

                // Ignore any virtual/database generated properties marked via attribute
                var generatedAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (generatedAttr != null && generatedAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed)
                    continue;

                yield return $"{columnAttr.Name} AS {prop.Name}";
            }
        }

        /// <summary>
        /// Get all public property values of an object.
        /// </summary>
        /// <typeparam name="TEntity">Generic type.</typeparam>
        /// <param name="entity">Entity data model to retrieve property values from.</param>
        /// <param name="includedProperties">List of property names to retrieve values of.</param>
        /// <param name="ignoredProperties">List of property names to ignore retrieving values of.</param>
        /// <returns>Returns a list of object values.</returns>
        public static IEnumerable<object> GetPropertyValues<TEntity>(
            this TEntity entity,
            IEnumerable<string>? includedProperties = null,
            IEnumerable<string>? ignoredProperties = null)
        {
            // Include only public instance properties as well as base inherited properties
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var properties = entity!.GetType().GetProperties(flags);
            //var properties = TypeDescriptor.GetProperties(typeof(TEntity));

            foreach (var prop in properties)
            {
                // Ignore any properties specified that match the entity
                if ((ignoredProperties?.Contains(prop.Name) ?? false) ||
                    !(includedProperties?.Contains(prop.Name) ?? true))
                    continue;

                // Ignore any properties that are not mapped to a database table via an attribute
                // or if the property is explicitly set to not be mapped.
                if (prop.GetCustomAttribute<ColumnAttribute>() == null)// ||
                                                                       //prop.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;

                // Ignore any virtual/database generated properties marked via attribute
                var generatedAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (generatedAttr != null && generatedAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed)
                    continue;

                // Check if property type is an enum and check if it's an enum we want to convert to an integer
                var value = prop.GetValue(entity);
                var safeValue = SqlifyPropertyValue(prop, value);
                yield return safeValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object SqlifyPropertyValue(this PropertyInfo property, object? value)
        {
            if (value == null)
            {
                return DbNull;
            }

            // Convert enums
            if (property.PropertyType.IsEnum)
            {
                return SqlifyPropertyValueToString(property, value);
            }
            // Convert strings
            else if (property.PropertyType == typeof(string) && value != null)
            {
                return SqlifyPropertyValueToString(value);
            }
            else if (property.PropertyType == typeof(SeenType))
            {
                return SqlifyPropertyValueToString(value);
            }
            // Convert generics, arrays, dictionaries, and lists
            else if (property.PropertyType.IsArray ||
                     (property.PropertyType.IsGenericType &&
                     _typesToConvertJson.Contains(property.PropertyType.GetGenericTypeDefinition())))
            {
                // Convert arrays and dictionaries to json strings
                var json = value.ToJson();
                return $"'{json}'";
            }

            return value ?? DbNull; //DBNull.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlifyPropertyValueToString(this object? value)
        {
            if (value == null)
            {
                return DbNull;
            }

            var unsafeValue = Convert.ToString(value);
            if (string.IsNullOrEmpty(unsafeValue))
            {
                return DbNull;
            }

            if (unsafeValue.Contains('\''))
            {
                unsafeValue = unsafeValue.Replace("'", @"\'");
            }
            if (unsafeValue.Contains('"'))
            {
                unsafeValue = unsafeValue.Replace('"', '\"');
            }

            var safeValue = $"'{unsafeValue}'";
            return safeValue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <param name="enumsToConvert"></param>
        /// <returns></returns>
        public static string SqlifyPropertyValueToString(this PropertyInfo property, object? value)
        {
            if (!(_enumsToConvert?.ContainsKey(property.PropertyType) ?? false))
            {
                // Treat/wrap enumeration value string in quotations
                return SqlifyPropertyValueToString(value);
            }

            // Convert enumeration value from string to specific type
            // using converter function
            var enumValue = Convert.ToString(value) ?? string.Empty;
            var propertyDescriptor = property.GetPropertyDescriptor();
            var converted = propertyDescriptor.Converter.ConvertFromString(enumValue);
            var convertedValue = _enumsToConvert[property.PropertyType](converted);
            var result = SqlifyPropertyValueToString(convertedValue);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static PropertyDescriptor GetPropertyDescriptor(this PropertyInfo propertyInfo)
        {
            var properties = TypeDescriptor.GetProperties(propertyInfo.DeclaringType);
            var propertyDescriptor = properties.Find(propertyInfo.Name, ignoreCase: false);
            return propertyDescriptor;
        }
    }
}