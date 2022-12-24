namespace ChuckDeviceController.Data.Repositories;

using MySqlConnector;

using ChuckDeviceController.Data.Entities;

public class GenericEntityDapperRepository<TEntity> : GenericDapperRepository<TEntity>, IGenericEntityRepository<TEntity>
    where TEntity : BaseEntity
{
    public GenericEntityDapperRepository(MySqlConnection connection)
        : base(connection)
    {
    }
}