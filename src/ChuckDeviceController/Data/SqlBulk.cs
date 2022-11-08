namespace ChuckDeviceController.Data
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Reflection;

    using Dapper;
    using Team = POGOProtos.Rpc.Team;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;

    public class SqlBulk
    {
        private const string DbNull = "NULL";

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
                //using var connection = new MySqlConnection(connectionString);
                var sqls = GenerateSqlQueryBatches(sqlQuery, sqlValues, entities);
                batchCount = sqls.Count();

                foreach (var (sql, args) in sqls)
                {
                    // TODO: Use Stored Procedure for upsert queries
                    var cmdDef = new CommandDefinition(sql, (object)args, commandTimeout: 10);
                    //rowsAffected += await _connection.ExecuteAsync(cmdDef);
                    rowsAffected += await EntityRepository.ExecuteAsync(sql, (object)args, commandTimeout: 10);
                    //await connection.ExecuteAsync(sql, (object)args);
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
                //using var connection = new MySqlConnection(connectionString);
                sqls = GenerateSqlQueryBatchesRaw(sqlQuery, sqlValues, entities);//, ignoredProperties: new List<string> { nameof(Cell.Updated) });
                batchCount = sqls.Count();

                foreach (var sql in sqls)
                {
                    //rowsAffected += await _connection.ExecuteAsync(sql);
                    rowsAffected += await EntityRepository.ExecuteAsync(sql);
                }
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
        private static IEnumerable<(string, dynamic)> GenerateSqlQueryBatches<TEntity>(
            string sqlQuery,
            string sqlValues,
            IEnumerable<TEntity> entities,
            ushort batchSize = 1000)
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

        private static IEnumerable<string> GenerateSqlQueryBatchesRaw<TEntity>(
            string sqlQuery,
            string sqlValues,
            IEnumerable<TEntity> entities,
            ushort batchSize = 1000,
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

        private static IEnumerable<object> GetPropertyValues<TEntity>(TEntity entity, IEnumerable<string>? ignoredProperties = null)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var properties = entity!.GetType().GetProperties(flags);
            //var properties = TypeDescriptor.GetProperties(typeof(TEntity));
            foreach (var prop in properties)
            {
                // Ignore any properties that are not mapped to a database table via an attribute
                if (prop.GetCustomAttribute<ColumnAttribute>() == null ||
                    prop.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;

                // Ignore any virtual/database generated properties marked via attribute
                var generatedAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (generatedAttr != null && generatedAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed)
                    continue;

                // Ignore any properties specified that match the entity
                if (ignoredProperties?.Contains(prop.Name) ?? false)
                    continue;

                // Check if property type is an enum and check if it's an enum we want to convert to an integer
                var value = prop.GetValue(entity) ?? null;
                if (prop.PropertyType.IsEnum && _enumsToConvert.ContainsKey(prop.PropertyType))
                {
                    var enumValue = Convert.ToString(value) ?? string.Empty;
                    var propertyDescriptor = GetPropertyDescriptor(prop);
                    var convertedValue = Convert.ToInt32(propertyDescriptor.Converter.ConvertFromString(enumValue));
                    yield return convertedValue;
                }
                else if (prop.PropertyType == typeof(string) && value != null)
                {
                    var strValue = Convert.ToString(value) ?? string.Empty;
                    if (strValue.Contains('"') || strValue.Contains('\''))
                    {
                        strValue = $"`{strValue}`";
                    }
                    else
                    {
                        strValue = $"'{strValue}'";
                    }
                    yield return strValue;
                }
                else
                {
                    value ??= DbNull; //DBNull.Value;
                    yield return value;
                }
            }
        }

        #endregion

        #region Helper Methods

        public static PropertyDescriptor GetPropertyDescriptor(PropertyInfo propertyInfo)
        {
            var properties = TypeDescriptor.GetProperties(propertyInfo.DeclaringType);
            var propertyDescriptor = properties.Find(propertyInfo.Name, ignoreCase: false);
            return propertyDescriptor;
        }

        #endregion
    }
}