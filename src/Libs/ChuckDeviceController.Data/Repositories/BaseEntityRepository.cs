namespace ChuckDeviceController.Data.Repositories;

using System.Data;

using MicroOrm.Dapper.Repositories;
using MicroOrm.Dapper.Repositories.SqlGenerator;
using MySqlConnector;

using ChuckDeviceController.Data.Entities;

public class BaseEntityRepository<TEntity> : DapperRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly MySqlConnection _connection;

    public BaseEntityRepository(MySqlConnection connection, ISqlGenerator<TEntity> sqlGenerator)
        : base(connection, sqlGenerator)
    {
        _connection = connection;
    }

    protected IDbConnection GetConnection()
    {
        return _connection;
    }
}