namespace ChuckDeviceController.Data.Repositories.Dapper;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using global::Dapper;
using MicroOrm.Dapper.Repositories.SqlGenerator;
using MicroOrm.Dapper.Repositories.SqlGenerator.Filters;
using MySqlConnector;

using ChuckDeviceController.Data.Factories;
using ChuckDeviceController.Data.Translators;
using ChuckDeviceController.Data.TypeHandlers;
using ChuckDeviceController.Extensions;

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
    private const string LimitQuery = " LIMIT {0}";
    private const string CountQuery = "SELECT COUNT(*) FROM {0}";

    #endregion

    #region Variables

    private static IEnumerable<PropertyInfo> GetProperties => typeof(TEntity).GetProperties();
    private static readonly Func<Type, string> GetTableName = (o) => typeof(TEntity).GetTableAttribute() ?? nameof(TEntity).ToLower();
    private static readonly Func<string, string> ParamTemplateFunc = new(property => $"@{property}");
    private static readonly IEnumerable<string> _reservedKeywords = new[]
    {
        "group",
        "key",
        "character",
    };

    private readonly IMySqlConnectionFactory _factory = null!;
    private readonly ISqlGenerator<TEntity> _sqlGenerator;
    private readonly MySqlQueryTranslator _translator;
    private readonly string _tableName;
    private readonly string _keyName;

    #endregion

    #region Constructors

    protected DapperGenericRepository(IMySqlConnectionFactory factory)
        : this(GetTableName(typeof(TEntity)), factory)
    {
    }

    protected DapperGenericRepository(string tableName, IMySqlConnectionFactory factory)
    {
        DapperTypeMappings.AddTypeMappers();

        _tableName = tableName;
        _factory = factory;
        _keyName = typeof(TEntity).GetKeyAttribute()!;
        _sqlGenerator = new SqlGenerator<TEntity>(SqlProvider.MySQL, useQuotationMarks: true);
        _translator = new MySqlQueryTranslator(_reservedKeywords);
    }

    #endregion

    #region Public Methods

    public bool Any(Expression<Func<TEntity, bool>>? predicate = null)
    {
        using var connection = CreateConnectionAsync().Result;
        var filterData = new FilterData
        {
            SelectInfo = new SelectInfo
            {
                Columns = new() { _keyName },
                Permanent = false,
            },
        };
        var (sql, param) = GenerateSelectQuery(predicate, filterData);
        sql += string.Format(LimitQuery, 1);
        var result = connection.QueryFirstOrDefault<TEntity>(sql);
        return result != null;
    }

    public int Count(Expression<Func<TEntity, bool>>? predicate = null, params Expression<Func<TEntity, object>>[] includes)
    {
        using var connection = CreateConnectionAsync().Result;
        var query = _sqlGenerator.GetCount(predicate, includes);
        var sql = query.SqlBuilder.ToString();
        var result = connection.ExecuteScalar<int>(sql);
        return result;
    }

    #region Select Methods

    public async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken stoppingToken = default)
    {
        var properties = GenerateListOfProperties(GetProperties);
        var columnNames = properties.Keys.ToList();
        var filterData = new FilterData
        {
            SelectInfo = new SelectInfo
            {
                Columns = columnNames,
                Permanent = false,
            },
        };
        var (sql, param) = GenerateSelectQuery(predicate, filterData);

        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.QueryFirstOrDefaultAsync<TEntity>(sql, param);
        return result;
    }

    public async Task<IEnumerable<TEntity>> FindAllAsync(CancellationToken stoppingToken = default)
    {
        using var connection = await CreateConnectionAsync(stoppingToken);
        return await connection.QueryAsync<TEntity>(string.Format(SelectQuery, _tableName));
    }

    public async Task<TEntity> FindAsync(TKey id, CancellationToken stoppingToken = default)
    {
        var (primaryKeyProperty, primaryKeyColumnName) = GetProperties.GetPrimaryKey();
        var parameters = new DynamicParameters();
        parameters.Add(primaryKeyColumnName, id);

        var selectQuery = string.Format(SelectWhereQuery, _tableName, primaryKeyColumnName, primaryKeyProperty?.Name);
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.QuerySingleOrDefaultAsync<TEntity>(selectQuery, parameters);
        return result;
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken stoppingToken = default)
    {
        var properties = GenerateListOfProperties(GetProperties);
        var columnNames = properties.Keys.ToList();
        var filterData = new FilterData
        {
            SelectInfo = new SelectInfo
            {
                Columns = columnNames,
                Permanent = false,
            },
        };
        var (sql, param) = GenerateSelectQuery(predicate, filterData);

        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.QueryAsync<TEntity>(sql, param);
        return result;
    }

    #endregion

    #region Insert Methods

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

    #endregion

    #region Update Methods

    public async Task<int> UpdateAsync(TEntity entity, CancellationToken stoppingToken = default)
    {
        var updateQuery = GenerateUpdateQuery();
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(updateQuery, entity);
        return result;
    }

    public async Task<int> UpdateAsync(
        TEntity entity,
        Dictionary<string, Func<TEntity, object?>> mappings,
        CancellationToken stoppingToken = default)
    {
        var updateQuery = GenerateUpdateQuery(entity, mappings, out var parameters);
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(updateQuery, parameters);
        return result;
    }

    public async Task<int> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken stoppingToken = default)
    {
        var query = _sqlGenerator.GetBulkUpdate(entities);
        var updateQuery = query.SqlBuilder.ToString();
        var parameters = query.Param;

        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(updateQuery, parameters);
        return result;
    }

    public async Task<int> UpdateRangeAsync(
        IEnumerable<TEntity> entities,
        Dictionary<string, Func<TEntity, object?>> mappings,
        CancellationToken stoppingToken = default)
    {
        var updateQuery = new List<string>();
        var updateParams = new List<DynamicParameters>();

        foreach (var entity in entities)
        {
            var query = GenerateUpdateQuery(entity, mappings, out var parameters);
            updateQuery.Add(query);
            updateParams.Add(parameters);
        }

        var sql = string.Join(";", updateQuery);
        using var connection = await CreateConnectionAsync(stoppingToken);
        var result = await connection.ExecuteAsync(sql, updateParams);
        return result;
    }

    #endregion

    #region Delete Methods

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

    #endregion

    #region Query Generator Methods

    private (string, object?) GenerateSelectQuery(
        Expression<Func<TEntity, bool>>? predicate,
        FilterData? filterData,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var query = _sqlGenerator.GetSelectAll(null, filterData, includes);

        _ = _translator.Translate(predicate!);

        if (!string.IsNullOrEmpty(_translator.WhereClause))
        {
            query.SqlBuilder.Append(" WHERE ");
            query.SqlBuilder.Append(_translator.WhereClause);
        }
        if (!string.IsNullOrEmpty(_translator.OrderBy))
        {
            query.SqlBuilder.Append(" ORDER BY ");
            query.SqlBuilder.Append(_translator.OrderBy);
        }
        if (_translator.Skip != null)
        {
            query.SqlBuilder.Append(" LIMIT ");
            query.SqlBuilder.Append(_translator.Skip);
        }
        if (_translator.Take != null)
        {
            query.SqlBuilder.Append(" OFFSET ");
            query.SqlBuilder.Append(_translator.Take);
        }
        //query.SqlBuilder.Append(" WHERE ");
        //query.SqlBuilder.Append(whereExpression);

        var sql = query.SqlBuilder.ToString();
        var param = query.Param;
        return (sql, param);
    }

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
            if (!property.IsPrimaryKey())
            {
                var paramName = ParamTemplateFunc(property.Name);
                updateQuery.Append($"{columnKey}={paramName},");
            }
        }

        var (primaryKey, primaryColumnName) = properties.Values.GetPrimaryKey();
        updateQuery.Remove(updateQuery.Length - 1, 1); // Remove last comma
        updateQuery.Append(string.Format(WhereQuery, primaryColumnName, primaryKey?.Name));

        var sql = updateQuery.ToString();
        return sql;
    }

    private string GenerateUpdateQuery(
        TEntity entity,
        Dictionary<string, Func<TEntity, object?>> mappings,
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
            if (!property.IsPrimaryKey())
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
        var (primaryKeyProperty, primaryKeyColumnName) = GetProperties.GetPrimaryKey();
        var deleteQuery = new StringBuilder(string.Format(DeleteQuery, _tableName, primaryKeyColumnName, primaryKeyProperty?.Name));
        parameters = new DynamicParameters();
        parameters.Add(primaryKeyColumnName, id);

        var sql = deleteQuery.ToString();
        return sql;
    }

    private string GenerateDeleteQuery(IEnumerable<TKey> ids, out DynamicParameters parameters)
    {
        var (primaryKeyProperty, primaryKeyColumnName) = GetProperties.GetPrimaryKey();
        var deleteQuery = new StringBuilder(string.Format(DeleteRangeQuery, _tableName, primaryKeyColumnName, primaryKeyProperty?.Name));
        var paramName = ParamTemplateFunc(primaryKeyProperty!.Name);
        parameters = new DynamicParameters();
        parameters.Add(paramName, ids);

        var sql = deleteQuery.ToString();
        return sql;
    }

    #endregion

    #region Private Methods

    private static (string?, string?, object?) GetPrimaryKey(TEntity entity, IEnumerable<PropertyInfo> properties)
    {
        var (primaryKeyProperty, primaryKeyColumnName) = properties.GetPrimaryKey();
        var primaryKeyValue = primaryKeyProperty?.GetValue(entity);
        return (primaryKeyProperty?.Name, primaryKeyColumnName, primaryKeyValue);
    }

    private async Task<MySqlConnection> CreateConnectionAsync(CancellationToken stoppingToken = default)
    {
        //var connection = new MySqlConnection(_connectionString);
        //await connection.OpenAsync(stoppingToken);
        //return connection;
        var connection = await _factory.CreateConnectionAsync(open: true, stoppingToken);
        return connection;
    }

    private static Dictionary<string, PropertyInfo> GenerateListOfProperties(IEnumerable<PropertyInfo> properties)
    {
        var result = new Dictionary<string, PropertyInfo>();
        foreach (var property in properties)
        {
            // Ignore any properties not marked with column attribute
            var columnName = property.GetColumnAttribute();
            if (string.IsNullOrEmpty(columnName))
                continue;

            // Ignore any virtual/database generated properties marked via attribute
            if (property.IsGeneratedColumn())
                continue;

            if (_reservedKeywords.Contains(columnName))
            {
                result.Add($"`{columnName}`", property);
            }
            else
            {
                result.Add(columnName, property);
            }
        }
        return result;
    }

    #endregion
}