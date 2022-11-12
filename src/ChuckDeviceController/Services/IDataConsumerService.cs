namespace ChuckDeviceController.Services
{
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;

    public interface IDataConsumerService
    {
        Task AddEntityAsync(SqlQueryType query, BaseEntity entity);

        Task AddEntitiesAsync(SqlQueryType query, IEnumerable<BaseEntity> entities);
    }
}