namespace ChuckDeviceController.Data
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Reflection;

    using Team = POGOProtos.Rpc.Team;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions.Json;
    using System.Diagnostics;

    // TODO: Use nvarchar instead of varchar for string columns
    // TODO: Use Stored Procedure for upsert queries

    public class SqlBulk
    {
        #region Constants

        private const string DbNull = "NULL";
        private const int DefaultMaxBatchSize = 1000;

        #endregion

        #region Variables

        private static readonly SemaphoreSlim _sem = new(1, 1);
        private static readonly ILogger<SqlBulk> _logger =
            new Logger<SqlBulk>(LoggerFactory.Create(options => options.SetMinimumLevel(LogLevel.Debug)));
        private static readonly IReadOnlyDictionary<Type, Type> _enumsToConvert = new Dictionary<Type, Type>
        {
            { typeof(WeatherCondition), typeof(int) },
            { typeof(Team), typeof(int) },
        };

        #endregion

        #region Bulk Insert

        /// <summary>
        /// Parameterized
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sqlQuery"></param>
        /// <param name="sqlValues"></param>
        /// <param name="entities"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task<SqlBulkResult> InsertInBulkAsync<TEntity>(
            string sqlQuery,
            string sqlValues,
            IEnumerable<TEntity> entities,
            CancellationToken stoppingToken = default)
            where TEntity : BaseEntity
        {
            await _sem.WaitAsync(stoppingToken);

            bool success;
            var rowsAffected = 0;
            var batchCount = 0;
            //var expectedCount = entities.Count(); // TODO: Remove

            try
            {
                var sqls = GenerateSqlQueryBatches(sqlQuery, sqlValues, entities);
                batchCount = sqls.Count();
                foreach (var (sql, args) in sqls)
                {
                    rowsAffected += await EntityRepository.ExecuteAsync(sql, (object)args, commandTimeoutS: 30);
                }
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError($"Error: {ex}");
            }

            _sem.Release();
            return new SqlBulkResult(success, batchCount, rowsAffected, 0);
        }

        /// <summary>
        /// Raw
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sqlQuery"></param>
        /// <param name="sqlValues"></param>
        /// <param name="entities"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        public async Task<SqlBulkResult> InsertInBulkRawAsync<TEntity>(
            string sqlQuery,
            string sqlValues,
            IEnumerable<TEntity> entities,
            CancellationToken stoppingToken = default)
            where TEntity : BaseEntity
        {
            await _sem.WaitAsync(stoppingToken);

            bool success;
            var rowsAffected = 0;
            var batchCount = 0;
            //var expectedCount = entities.Count(); // TODO: Remove
            IEnumerable<string>? sqls = default;

            try
            {
                sqls = GenerateSqlQueryBatchesRaw(sqlQuery, sqlValues, entities);//, ignoredProperties: new List<string> { nameof(Cell.Updated) });
                batchCount = sqls.Count();
                rowsAffected += await EntityRepository.ExecuteAsync(sqls, commandTimeoutS: 30, stoppingToken);
                //foreach (var sql in sqls)
                //{
                //    rowsAffected += await EntityRepository.ExecuteAsync(sql);
                //}
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError($"Error: {ex}");
            }

            _sem.Release();
            return new SqlBulkResult(success, batchCount, rowsAffected, rowsAffected);
        }

        #endregion

        #region Generate SQL Query Methods

        // https://stackoverflow.com/a/56250588

        /// <summary>
        /// Generates a SQL query in batches using parameterized queries.
        /// </summary>
        /// <typeparam name="TEntity">Entity data model</typeparam>
        /// <param name="sqlQuery">SQL query</param>
        /// <param name="sqlValues">SQL insert query string</param>
        /// <param name="entities">Entities to insert</param>
        /// <param name="batchSize">Maximum number of entities per batch</param>
        /// <returns>Returns a list of tuples containing the SQL query and the entities to pass as an argument.</returns>
        private static IEnumerable<(string, dynamic)> GenerateSqlQueryBatches<TEntity>(
            string sqlQuery,
            string sqlValues,
            IEnumerable<TEntity> entities,
            int batchSize = DefaultMaxBatchSize)
            where TEntity : BaseEntity
        {
            var sqlsToExecute = new List<(string, dynamic)>();
            var batchCount = (int)Math.Ceiling((double)entities.Count() / batchSize);

            for (var i = 0; i < batchCount; i++)
            {
                var entityBatch = entities.Skip(i * batchSize).Take(batchSize);
                var queryValues = string.Join(',', entityBatch.Select(x => sqlValues));
                var query = string.Format(sqlQuery, queryValues);
                sqlsToExecute.Add((query, entityBatch));
            }

            return sqlsToExecute;
        }

        /// <summary>
        /// Generates a SQL query in batches using raw (possible SQL injection prone) queries.
        /// </summary>
        /// <typeparam name="TEntity">Entity data model</typeparam>
        /// <param name="sqlQuery">SQL query</param>
        /// <param name="sqlValues">SQL insert query string</param>
        /// <param name="entities">Entities to insert</param>
        /// <param name="batchSize">Maximum number of entities per batch</param>
        /// <returns>Returns a list of SQL queries including the entity data.</returns>
        private static IEnumerable<string> GenerateSqlQueryBatchesRaw<TEntity>(
            string sqlQuery,
            string sqlValues,
            IEnumerable<TEntity> entities,
            int batchSize = DefaultMaxBatchSize,
            IEnumerable<string>? ignoredProperties = null)
            where TEntity : BaseEntity
        {
            var batchCount = (int)Math.Ceiling((double)entities.Count() / batchSize);
            var sqlQueries = new List<string>();

            for (var i = 0; i < batchCount; i++)
            {
                var entityBatch = entities.Skip(i * batchSize).Take(batchSize);
                var queryValues = string.Join(',', entityBatch.Select(entity =>
                {
                    var propValues = GetPropertyValues(entity, ignoredProperties);
                    var result = string.Format(sqlValues, propValues.ToArray());
                    return result;
                }));
                var query = string.Format(sqlQuery, queryValues);
                sqlQueries.Add(query);
            }

            return sqlQueries;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <param name="ignoredProperties"></param>
        /// <returns></returns>
        private static IEnumerable<object> GetPropertyValues<TEntity>(TEntity entity, IEnumerable<string>? ignoredProperties = null)
        {
            // Include only public instance properties as well as base inherited properties
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var properties = entity!.GetType().GetProperties(flags);
            //var properties = TypeDescriptor.GetProperties(typeof(TEntity));

            foreach (var prop in properties)
            {
                // Ignore any properties specified that match the entity
                if (ignoredProperties?.Contains(prop.Name) ?? false)
                    continue;

                // Ignore any properties that are not mapped to a database table via an attribute
                // or if the property is explicitly set to not be mapped.
                if (prop.GetCustomAttribute<ColumnAttribute>() == null ||
                    prop.GetCustomAttribute<NotMappedAttribute>() != null)
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

        private static object SqlifyPropertyValue(PropertyInfo property, object? value)
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
                         (
                             property.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                             property.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                             property.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                         )
                     ))
            {
                // Convert arrays and dictionaries to json strings
                var json = value.ToJson();
                return $"'{json}'";
            }

            return value ?? DbNull; //DBNull.Value;
        }

        private static string SqlifyPropertyValueToString(object? value)
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

        private static string SqlifyPropertyValueToString(PropertyInfo property, object? value, IReadOnlyDictionary<Type, Type>? enumsToConvert = null)
        {
            if (enumsToConvert?.ContainsKey(property.PropertyType) ?? false)
            {
                // Convert enumeration value from string to integer
                var enumValue = Convert.ToString(value) ?? string.Empty;
                var propertyDescriptor = GetPropertyDescriptor(property);
                var convertedValue = Convert.ToInt32(propertyDescriptor.Converter.ConvertFromString(enumValue));
                return convertedValue.ToString();
            }
            // Treat/wrap enumeration value string in quotations
            return SqlifyPropertyValueToString(value);
        }

        #endregion

        #region Helper Methods

        // TODO: Move to extensions class
        public static PropertyDescriptor GetPropertyDescriptor(PropertyInfo propertyInfo)
        {
            var properties = TypeDescriptor.GetProperties(propertyInfo.DeclaringType);
            var propertyDescriptor = properties.Find(propertyInfo.Name, ignoreCase: false);
            return propertyDescriptor;
        }

        #endregion
    }
}