namespace ChuckDeviceController.Data.Repositories.Dapper;

using MySqlConnector;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

public class GenericEntityDapperRepository<TEntity> : GenericDapperRepository<TEntity>, IGenericEntityRepository<TEntity>
    where TEntity : BaseEntity
{
    public GenericEntityDapperRepository(MySqlConnection connection)
        : base(connection)
    {
    }
}