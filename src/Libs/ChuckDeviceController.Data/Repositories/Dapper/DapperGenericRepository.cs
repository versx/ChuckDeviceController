namespace ChuckDeviceController.Data.Repositories.Dapper;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

using global::Dapper;
using MySqlConnector;

// Reference: https://itnext.io/generic-repository-pattern-using-dapper-bd48d9cd7ead
// Reference: https://github.com/phnx47/dapper-repositories
public abstract class DapperGenericRepository<TKey, TEntity> : IDapperGenericRepository<TKey, TEntity>
    where TKey : notnull
    where TEntity : class
{
    #region Query Template Constants

    private const string SelectQuery = "SELECT * FROM {0}";
    private const string SelectWhereQuery = "SELECT * FROM {0} WHERE {1}=@{2}";
    private const string InsertQuery = "INSERT INTO {0} ";
    private const string InsertValuesQuery = ") VALUES (";
    private const string UpdateQuery = "UPDATE {0} SET ";
    private const string DeleteQuery = "DELETE FROM {0} WHERE {1}=@{2}";
    private const string DeleteRangeQuery = "DELETE FROM {0} WHERE {1} IN @{2}";
    private const string WhereQuery = " WHERE {0}=@{1}";

    #endregion

    #region Variables

    private static IEnumerable<PropertyInfo> GetProperties => typeof(TEntity).GetProperties();
    private static readonly Func<string, string> ParamTemplateFunc = new(property => $"@{property}");

    private readonly string _tableName;
    private readonly string _connectionString;

    #endregion

    #region Constructor

    protected DapperGenericRepository(string tableName, string connectionString)
    {
        _tableName = tableName;
        _connectionString = connectionString;
    }

    #endregion

    #region Public Methods

    public async Task<IEnumerable<TEntity>> FindAllAsync(CancellationToken stoppingToken = default)
    {
        using var connection = await CreateConnectionAsync(stoppingToken);
        return await connection.QueryAsync<TEntity>(string.Format(SelectQuery, _tableName));
    }

    public async Task<TEntity> FindAsync(TKey id, CancellationToken stoppingToken = default)
    {
        var (primaryKeyProperty, primaryKeyColumnName) = GetPrimaryKey(GetProperties);
        var parameters = new DynamicParameters();
        parameters.Add(primaryKeyColumnName, id);

        var selectQuery = string.Format(SelectWhereQuery, _tableName, primaryKeyColumnName, primaryKeyProperty?.Name);
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.QuerySingleOrDefaultAsync<TEntity>(selectQuery, parameters);
        return result;
    }

    public async Task<int> InsertAsync(TEntity entity, CancellationToken stoppingToken = default)
    {
        var insertQuery = GenerateInsertQuery();
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(insertQuery, entity);
        return result;
    }

    public async Task<int> InsertRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken stoppingToken = default)
    {
        var insertQuery = GenerateInsertQuery();
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(insertQuery, entities);
        return result;
    }

    public async Task<int> UpdateAsync(TEntity entity, CancellationToken stoppingToken = default)
    {
        var updateQuery = GenerateUpdateQuery();
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(updateQuery, entity);
        return result;
    }

    public async Task<int> UpdateAsync(
        TEntity entity,
        Dictionary<string, Func<TEntity, object>> mappings,
        CancellationToken stoppingToken = default)
    {
        var updateQuery = GenerateUpdateQuery(entity, mappings, out var parameters);
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(updateQuery, parameters);
        return result;
    }

    public async Task<bool> DeleteAsync(TKey id, CancellationToken stoppingToken = default)
    {
        var deleteQuery = GenerateDeleteQuery(id, out var parameters);
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(deleteQuery, parameters);
        return result > 0;
    }

    public async Task<bool> DeleteRangeAsync(
        IEnumerable<TKey> ids,
        CancellationToken stoppingToken = default)
    {
        var deleteQuery = GenerateDeleteQuery(ids, out var parameters);
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(deleteQuery, parameters);
        return result > 0;
    }

    #endregion

    #region Query Generator Methods

    private string GenerateInsertQuery()
    {
        var insertQuery = new StringBuilder(string.Format(InsertQuery, _tableName));
        var properties = GenerateListOfProperties(GetProperties);

        insertQuery.Append('(');
        properties.Keys.ToList().ForEach(prop => insertQuery.Append($"{prop},"));

        insertQuery
            .Remove(insertQuery.Length - 1, 1)
            .Append(InsertValuesQuery);

        properties.Values.ToList().ForEach(prop => insertQuery.Append(ParamTemplateFunc(prop.Name) + ","));

        insertQuery
            .Remove(insertQuery.Length - 1, 1)
            .Append(')');

        var sql = insertQuery.ToString();
        return sql;
    }

    private string GenerateUpdateQuery()
    {
        var updateQuery = new StringBuilder(string.Format(UpdateQuery, _tableName));
        var properties = GenerateListOfProperties(GetProperties);

        foreach (var (columnKey, property) in properties)
        {
            if (!IsPrimaryKey(property))
            {
                var paramName = ParamTemplateFunc(property.Name);
                updateQuery.Append($"{columnKey}=@{paramName},");
            }
        }

        var (primaryKey, primaryColumnName) = GetPrimaryKey(properties.Values);
        updateQuery.Remove(updateQuery.Length - 1, 1); // Remove last comma
        updateQuery.Append(string.Format(WhereQuery, primaryColumnName, primaryKey?.Name));

        var sql = updateQuery.ToString();
        return sql;
    }

    private string GenerateUpdateQuery(
        TEntity entity,
        Dictionary<string, Func<TEntity, object>> mappings,
        out DynamicParameters parameters)
    {
        var updateQuery = new StringBuilder(string.Format(UpdateQuery, _tableName));
        var properties = GenerateListOfProperties(GetProperties);
        var columns = new List<string>();
        parameters = new DynamicParameters();

        foreach (var (columnName, valueFunc) in mappings)
        {
            var property = properties[columnName];
            var paramName = ParamTemplateFunc(property.Name);
            if (!IsPrimaryKey(property))
            {
                columns.Add($"{columnName}={paramName}");
            }

            parameters.Add(paramName, valueFunc(entity));
        }

        var (primaryKeyName, primaryKeyColumnName, primaryKeyValue) = GetPrimaryKey(entity, properties.Values);
        parameters.Add(primaryKeyColumnName, primaryKeyValue);

        updateQuery.AppendLine(string.Join(", ", columns));
        updateQuery.Append(string.Format(WhereQuery, primaryKeyColumnName, primaryKeyName));

        var sql = updateQuery.ToString();
        return sql;
    }

    private string GenerateDeleteQuery(TKey id, out DynamicParameters parameters)
    {
        var (primaryKeyProperty, primaryKeyColumnName) = GetPrimaryKey(GetProperties);
        var deleteQuery = new StringBuilder(string.Format(DeleteQuery, _tableName, primaryKeyColumnName, primaryKeyProperty?.Name));
        parameters = new DynamicParameters();
        parameters.Add(primaryKeyColumnName, id);

        var sql = deleteQuery.ToString();
        return sql;
    }

    private string GenerateDeleteQuery(IEnumerable<TKey> ids, out DynamicParameters parameters)
    {
        var (primaryKeyProperty, primaryKeyColumnName) = GetPrimaryKey(GetProperties);
        var deleteQuery = new StringBuilder(string.Format(DeleteRangeQuery, _tableName, primaryKeyColumnName, primaryKeyProperty?.Name));
        var paramName = ParamTemplateFunc(primaryKeyProperty!.Name);
        parameters = new DynamicParameters();
        parameters.Add(paramName, ids);

        var sql = deleteQuery.ToString();
        return sql;
    }

    #endregion

    #region Private Methods

    private static bool IsPrimaryKey(PropertyInfo property)
    {
        return property.GetCustomAttribute<KeyAttribute>() != null;
    }

    private static (string?, string?, object?) GetPrimaryKey(TEntity entity, IEnumerable<PropertyInfo> properties)
    {
        var (primaryKeyProperty, primaryKeyColumnName) = GetPrimaryKey(properties);
        var primaryKeyValue = primaryKeyProperty?.GetValue(entity);
        return (primaryKeyProperty?.Name, primaryKeyColumnName, primaryKeyValue);
    }

    private static (PropertyInfo?, string?) GetPrimaryKey(IEnumerable<PropertyInfo> properties)
    {
        var key = properties.FirstOrDefault(IsPrimaryKey);
        var colAttr = key?.GetCustomAttribute<ColumnAttribute>();
        var colName = colAttr?.Name;
        return (key, colName);
    }

    private async Task<MySqlConnection> CreateConnectionAsync(CancellationToken stoppingToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(stoppingToken);
        return connection;
    }

    private static Dictionary<string, PropertyInfo> GenerateListOfProperties(IEnumerable<PropertyInfo> properties)
    {
        var result = new Dictionary<string, PropertyInfo>();
        foreach (var property in properties)
        {
            // Ignore any properties not marked with column attribute
            var attr = property.GetCustomAttribute<ColumnAttribute>();
            if (attr?.Name == null)
                continue;

            // Ignore any virtual/database generated properties marked via attribute
            var genAttr = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
            if (genAttr?.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed)
                continue;

            result.Add(attr.Name, property);
        }
        return result;
    }

    #endregion
}