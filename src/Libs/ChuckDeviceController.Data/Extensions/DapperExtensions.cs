namespace ChuckDeviceController.Data.Extensions;

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
    private const string UpdateBulkQuery = "UPDATE {0} SET {1} WHERE {2}";

    public static async Task<int> BulkInsertAsync<TEntity>(
        this MySqlConnection connection,
        string tableName,
        IEnumerable<TEntity> entities,
        ColumnDataExpression<TEntity> dataFunc,
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
                rowsAffected += await connection.ExecuteAsync(sql, args, transaction: trans, commandTimeout: MaxCommandTimeoutS, commandType: CommandType.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DapperExtensions::BulkInsertAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        return rowsAffected;
    }

    //public static async Task<int> BulkUpdateAsync<TEntity>(
    //    this MySqlConnection connection,
    //    string tableName,
    //    IEnumerable<TEntity> entities,
    //    ColumnDataExpression<TEntity> dataFunc,
    //    IDbTransaction trans,
    //    IEnumerable<string> whereIds)
    //{
    //    var batchSize = Math.Min((int)Math.Ceiling((double)MaxParameterSize / dataFunc.Keys.Count), MaxBatchSize);
    //    var totalCount = entities.Count();
    //    var batchCount = (int)Math.Ceiling((double)totalCount / batchSize);
    //    var columnValues = string.Join(", ", dataFunc.Select(x => x.Value(default).ToString()));
    //    var whereSql = "id IN (" + string.Join(", ", whereIds.Select(x => $"'{x}'")) + ")";
    //    var updateSql = string.Format(UpdateBulkQuery, tableName, string.Join(", ", columnValues), whereSql);
    //    var sqlToExecute = new List<Tuple<string, DynamicParameters>>();
    //    var rowsAffected = 0;

    //    foreach (var (sql, args) in sqlToExecute)
    //    {
    //        try
    //        {
    //            rowsAffected += await connection.ExecuteAsync(sql, args, transaction: trans, commandTimeout: MaxCommandTimeoutS, commandType: CommandType.Text);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"[DapperExtensions::BulkUpdateAsync] Error: {ex.InnerException?.Message ?? ex.Message}");
    //        }
    //    }

    //    return rowsAffected;
    //}

    private static Tuple<IEnumerable<string>, DynamicParameters> GetQueries<TEntity>(
        IEnumerable<TEntity> dataToInsert,
        ColumnDataExpression<TEntity> dataFunc)
    {
        var parameters = new DynamicParameters();

        return Tuple.Create(
            dataToInsert.Select(e => $"({string.Join(", ", GenerateQueryAndParameters(e, parameters, dataFunc))})"),
            parameters);
    }

    private static IEnumerable<string> GenerateQueryAndParameters<TEntity>(
        TEntity entity,
        DynamicParameters parameters,
        ColumnDataExpression<TEntity> dataFunc)
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
        var columns = columnNames.Select(columnName => $"{columnName}=VALUES({columnName})");
        var sb = new StringBuilder();
        sb.AppendLine(" ON DUPLICATE KEY UPDATE ");
        sb.AppendLine(string.Join(",", columns));
        var sql = sb.ToString();
        return sql;
    }
}

public class ColumnDataExpression<TEntity> : Dictionary<string, Func<TEntity, object>>
{
}