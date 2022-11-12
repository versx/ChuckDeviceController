namespace Dapper.Tests
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text;

    using Dapper;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using MySqlConnector;
    using Team = POGOProtos.Rpc.Team;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Json;

    public class CellInsertTests
    {
        private const int MaxInsertCount = 1000000;
        private const int DefaultMaxBatchSize = 1000;
        private const string ConnectionString = ""; // TODO: Add connection string to environment vars

        public const string CellOnMergeUpdate = @"
INSERT INTO s2cell (id, level, center_lat, center_lon, updated)
VALUES
    {0}
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    center_lat=VALUES(center_lat),
    center_lon=VALUES(center_lon),
    updated=VALUES(updated)
";
        public const string CellValuesRaw = "({0}, {1}, {2}, {3}, {4})";

        private MySqlConnection _connection = new(ConnectionString);

        [SetUp]
        public void Setup()
        {
            _connection = new MySqlConnection(ConnectionString);
            Task.Run(async () => await _connection.OpenAsync()).Wait();
        }

        /* 1 Million Rows Insert
         * Mine
         *   Without Trans:
         *   GetSqlsInBatches
         *   - 27s
         *   - 15s
         *   - 15s
         *   - 17s
         *   GenerateSqlQueryBatchesRaw
         *   - 21s
         *   - 35s
         *   - 37s
         *   - 35s
         *   - 31s
         *   - 33s (with GetPropertyValues)
         *   - 33s
         *   With Trans:
         *   - 57s
         *   - 33s
         *   - 31s (w/o GetPropertyValues)
         *   - 35s
        */

        [TestCase]
        public async Task TestCellsRaw()
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var cells = GenerateCells();
                await InsertBulkAsync(CellOnMergeUpdate, CellValuesRaw, cells);

                stopwatch.Stop();
                var seconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                Console.WriteLine($"[Cell] [Raw] Upserted {cells.Count:N0} S2 cells in {seconds}s");
                Assert.Pass();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cell] Error: {ex.InnerException?.Message ?? ex.Message}");
                Assert.Fail(ex.ToString());
            }
        }

        /* 1 Million Rows Insert
         * DapExt (~37s w/ or w/o transaction)
         *   Without Trans:
         *   - 44s
         *   - 42s
         *   - 40s
         *   - 37s
         *   With Trans:
         *   - 36s-ish
         *   - 40s
         *   - 40s
         *   - 37s
         */

        [TestCase]
        public async Task TestCellsParameterized()
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var cells = GenerateCells();
                await _connection.BulkInsertAsync("s2cell", cells, new DataUpdater<Cell>
                {
                    { "id", x => x.Id },
                    { "center_lat", x => x.Latitude },
                    { "center_lon", x => x.Longitude },
                    { "level", x => x.Level },
                    { "updated", x => x.Updated },
                });

                stopwatch.Stop();
                var seconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                Console.WriteLine($"[Cell] [Parameterized] Upserted {cells.Count:N0} S2 cells in {seconds}s");
                Assert.Pass();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cell] Error: {ex.InnerException?.Message ?? ex.Message}");
                Assert.Fail(ex.ToString());
            }
        }

        private static List<Cell> GenerateCells()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var cells = new List<Cell>();
            for (var i = 0; i < MaxInsertCount; i++)
            {
                cells.Add(new Cell
                {
                    Id = (ulong)i + 1,
                    Latitude = i + 1,
                    Longitude = i + 1,
                    Level = 15,
                    Updated = now,
                });
            }
            return cells;
        }

        private async Task InsertBulkAsync<TEntity>(string sqlQuery, string sqlValues, List<TEntity> entities)
            where TEntity : BaseEntity
        {
            using var trans = await _connection.BeginTransactionAsync();
            try
            {
                //var sqls = GetSqlsInBatches(entities);
                var sqls = GenerateSqlQueryBatchesRaw(sqlQuery, sqlValues, entities);
                foreach (var sql in sqls)
                {
                    //await _connection.ExecuteAsync(sql, commandTimeout: 30);
                    await _connection.ExecuteAsync(sql, transaction: trans, commandTimeout: 30);
                }
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
                await trans.RollbackAsync();
            }
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
            IEnumerable<string>? includedProperties = null,
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
                    var propValues = entity.GetPropertyValues(includedProperties, ignoredProperties);
                    var result = string.Format(sqlValues, propValues.ToArray());
                    return result;
                }));
                var query = string.Format(sqlQuery, queryValues);
                sqlQueries.Add(query);
            }

            return sqlQueries;
        }

        private IList<string> GetSqlsInBatches(List<Cell> entities, int batchSize = 1000)
        {
            var insertSql = "INSERT INTO s2cell (id, level, center_lat, center_lon, updated) VALUES ";
            var valuesSql = "('{0}', '{1}', '{2}', '{3}', '{4}')";
            var onDupSql = @"
ON DUPLICATE KEY UPDATE
    level=VALUES(level),
    center_lat=VALUES(center_lat),
    center_lon=VALUES(center_lon),
    updated=VALUES(updated)
";
            var sqlsToExecute = new List<string>();
            var numberOfBatches = (int)Math.Ceiling((double)entities.Count / batchSize);

            for (var i = 0; i < numberOfBatches; i++)
            {
                var entitiesToInsert = entities.Skip(i * batchSize).Take(batchSize);
                var valuesToInsert = entitiesToInsert.Select(x => string.Format(valuesSql, x.Id, x.Level, x.Latitude, x.Longitude, x.Updated));
                sqlsToExecute.Add(insertSql + string.Join(',', valuesToInsert) + onDupSql);
            }

            return sqlsToExecute;
        }
    }

    public class DataUpdater<TEntity> : Dictionary<string, Func<TEntity, object>>
    {
    }

    public static class DapperExtensions
    {
        private const int MaxBatchSize = 1000;
        private const int MaxParameterSize = 2000;
        private const int MaxCommandTimeoutS = 30;

        public static async Task<int> BulkInsertAsync<TEntity>(
            this MySqlConnection connection,
            string tableName,
            IEnumerable<TEntity> entities,
            DataUpdater<TEntity> dataFunc)
        {
            var batchSize = Math.Min((int)Math.Ceiling((double)MaxParameterSize / dataFunc.Keys.Count), MaxBatchSize);
            var totalCount = entities.Count();
            var numberOfBatches = (int)Math.Ceiling((double)totalCount / batchSize);
            var columnNames = dataFunc.Keys;
            var insertSql = $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ";
            var sqlToExecute = new List<Tuple<string, DynamicParameters>>();
            //var sqlToExecute = new List<Tuple<string, IEnumerable<TEntity>>>();
            var rowsAffected = 0;

            for (var i = 0; i < numberOfBatches; i++)
            {
                var dataToInsert = entities.Skip(i * batchSize)
                    .Take(batchSize);
                var (valueSql, args) = GetQueries(dataToInsert, dataFunc);

                var sql = $"{insertSql}{string.Join(", ", valueSql)}";
                //sql += GenerateOnDuplicateQuery(columnNames);
                sqlToExecute.Add(Tuple.Create(sql, args));
                //var sql = $"INSERT s2cell(id, level, center_lat, center_lon, updated) VALUES (@Id, @Level, @Latitude, @Longitude, @Updated)";
                //sqlToExecute.Add(Tuple.Create(sql, dataToInsert));
            }

            using var trans = await connection.BeginTransactionAsync();
            try
            {
                foreach (var (sql, args) in sqlToExecute)
                {
                    rowsAffected += await connection.ExecuteAsync(sql, args, transaction: trans, commandTimeout: MaxCommandTimeoutS);
                    //rowsAffected += await connection.ExecuteAsync(sql, args, commandTimeout: MaxCommandTimeoutS);
                }
                await trans.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.InnerException?.Message ?? ex.Message}");
                await trans.RollbackAsync();
            }

            return rowsAffected;
        }

        private static Tuple<IEnumerable<string>, DynamicParameters> GetQueries<TEntity>(
            IEnumerable<TEntity> dataToInsert,
            DataUpdater<TEntity> dataFunc)
        {
            var parameters = new DynamicParameters();

            return Tuple.Create(
                dataToInsert.Select(e => $"({string.Join(", ", GenerateQueryAndParameters(e, parameters, dataFunc))})"),
                parameters);
        }

        private static IEnumerable<string> GenerateQueryAndParameters<TEntity>(
            TEntity entity,
            DynamicParameters parameters,
            DataUpdater<TEntity> dataFunc)
        {
            var paramTemplateFunc = new Func<Guid, string>(guid => $"@p{guid.ToString().Replace("-", "")}");
            var paramList = new List<string>();

            foreach (var key in dataFunc)
            {
                var paramName = paramTemplateFunc(Guid.NewGuid());
                var value = key.Value(entity);
                parameters.Add(paramName, key.Value(entity));
                paramList.Add(paramName);
            }

            return paramList;
        }

        private static string GenerateOnDuplicateQuery(IEnumerable<string> columnNames)
        {
            var sb = new StringBuilder();
            sb.AppendLine(" ON DUPLICATE KEY UPDATE ");
            var columns = columnNames.Select(columnName => $"{columnName}=VALUES({columnName})");
            sb.AppendLine(string.Join(",", columns));
            var sql = sb.ToString();
            return sql;
        }
    }

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
                if (prop.GetCustomAttribute<ColumnAttribute>() == null)
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
            //else if (property.PropertyType.IsGenericType && property.PropertyType == typeof(Dictionary<,>))
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
        public static PropertyDescriptor? GetPropertyDescriptor(this PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                return null;

            var properties = TypeDescriptor.GetProperties(propertyInfo.DeclaringType);
            var propertyDescriptor = properties[propertyInfo.Name];
            return propertyDescriptor;
        }
    }
}