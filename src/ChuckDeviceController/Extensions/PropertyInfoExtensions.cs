namespace ChuckDeviceController.Extensions
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Reflection;

    using Team = POGOProtos.Rpc.Team;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Extensions.Json;

    public static class PropertyInfoExtensions
    {
        #region Constants

        private const string DbNull = "NULL";

        #endregion

        #region Variables

        private static readonly IReadOnlyDictionary<Type, Type> _enumsToConvert = new Dictionary<Type, Type>
        {
            { typeof(WeatherCondition), typeof(int) },
            { typeof(Team), typeof(int) },
        };
        private static readonly IEnumerable<Type> _typesToConvert = new[]
        {
            typeof(Dictionary<,>),
            typeof(List<>),
            typeof(IEnumerable<>),
        };

        #endregion

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
            if (entity == null)
            {
                // TODO: Remove bandaid
                yield return null;
            }
            else
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
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object SqlifyPropertyValue(
            this PropertyInfo property,
            object? value)
        {
            if (value == null)
            {
                return DbNull;
            }

            // Convert enums
            if (property.PropertyType.IsEnum)
            {
                return SqlifyPropertyValueToString(property, value, _enumsToConvert);
            }
            // Convert strings
            else if (property.PropertyType == typeof(string) && value != null)
            {
                return SqlifyPropertyValueToString(value);
            }
            // Convert generics, arrays, dictionaries, and lists
            else if (property.PropertyType.IsArray ||
                     (property.PropertyType.IsGenericType &&
                     _typesToConvert.Contains(property.PropertyType.GetGenericTypeDefinition())))
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

            var safeValue = unsafeValue.Contains('"') || unsafeValue.Contains('\'')
                // String value already contains quotations, use back ticks.
                ? $"`{unsafeValue}`"
                // Encapsulate string value in single quotations.
                : $"'{unsafeValue}'";

            return safeValue ?? DbNull;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <param name="enumsToConvert"></param>
        /// <returns></returns>
        public static string SqlifyPropertyValueToString(
            this PropertyInfo property,
            object? value,
            IReadOnlyDictionary<Type, Type>? enumsToConvert = null)
        {
            if (enumsToConvert?.ContainsKey(property.PropertyType) ?? false)
            {
                // Convert enumeration value from string to integer
                var enumValue = Convert.ToString(value) ?? string.Empty;
                var propertyDescriptor = property.GetPropertyDescriptor();
                var convertedValue = Convert.ToInt32(propertyDescriptor.Converter.ConvertFromString(enumValue));
                return convertedValue.ToString();
            }
            // Treat/wrap enumeration value string in quotations
            return SqlifyPropertyValueToString(value);
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