namespace ChuckDeviceController.Data.Repositories;

using System.Data;

using MicroOrm.Dapper.Repositories;
using MicroOrm.Dapper.Repositories.SqlGenerator;

using ChuckDeviceController.Data.Factories;

public class BaseEntityRepository<TEntity> : DapperRepository<TEntity>
    where TEntity : class
{
    private readonly IMySqlConnectionFactory _factory;

    public BaseEntityRepository(IMySqlConnectionFactory factory)
        : this(factory, new SqlGenerator<TEntity>(SqlProvider.MySQL, useQuotationMarks: true))
    {
    }

    public BaseEntityRepository(IMySqlConnectionFactory factory, ISqlGenerator<TEntity> generator)
        : base(factory.CreateConnection(), generator)
    {
        _factory = factory;
    }

    protected IDbConnection GetConnection()
    {
        return _factory.CreateConnection();
    }

    protected async Task<IDbConnection> GetConnectionAsync(CancellationToken stoppingToken = default)
    {
        return await _factory.CreateConnectionAsync(open: true, stoppingToken);
    }
}