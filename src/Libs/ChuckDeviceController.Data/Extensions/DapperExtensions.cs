namespace ChuckDeviceController.Data.Extensions
{
    using System.Data;
    using System.Text;

    using Dapper;
    using MySqlConnector;

    // Credits: https://stackoverflow.com/a/57442228
    public static class DapperExtensions
    {
        private const int MaxBatchSize = 1000;
        private const int MaxParameterSize = 2000;
        private const int MaxCommandTimeoutS = 30;
        private const string InsertBulkQuery = "INSERT INTO {0} ({1}) VALUES ";

        public static async Task<int> BulkInsertAsync<TEntity>(
            this MySqlConnection connection,
            string tableName,
            IEnumerable<TEntity> entities,
            DataUpdater<TEntity> dataFunc,
            IDbTransaction trans,
            bool includeOnDuplicateQuery)
        {
            var batchSize = Math.Min((int)Math.Ceiling((double)MaxParameterSize / dataFunc.Keys.Count), MaxBatchSize);
            var totalCount = entities.Count();
            var batchCount = (int)Math.Ceiling((double)totalCount / batchSize);
            var columnNames = dataFunc.Keys;
            var insertSql = string.Format(InsertBulkQuery, tableName, string.Join(", ", columnNames));
            var sqlToExecute = new List<Tuple<string, DynamicParameters>>();
            var rowsAffected = 0;

            for (var i = 0; i < batchCount; i++)
            {
                var dataToInsert = entities.Skip(i * batchSize)
                    .Take(batchSize);
                var (valueSql, args) = GetQueries(dataToInsert, dataFunc);
                var sql = insertSql + string.Join(", ", valueSql);
                if (includeOnDuplicateQuery)
                {
                    sql += GenerateOnDuplicateQuery(columnNames);
                }
                sqlToExecute.Add(Tuple.Create(sql, args));
            }

            foreach (var (sql, args) in sqlToExecute)
            {
                try
                {
                    rowsAffected += await connection.ExecuteAsync(sql, args, transaction: trans, commandTimeout: MaxCommandTimeoutS);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.InnerException?.Message ?? ex.Message}");
                }
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

    public class DataUpdater<TEntity> : Dictionary<string, Func<TEntity, object>>
    {
    }
}