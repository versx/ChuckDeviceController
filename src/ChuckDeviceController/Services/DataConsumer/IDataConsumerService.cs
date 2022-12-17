namespace ChuckDeviceController.Services.DataConsumer
{
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;

    public interface IDataConsumerService
    {
        Task AddEntityAsync(SqlQueryType type, BaseEntity entity);

        Task AddEntitiesAsync(SqlQueryType type, IEnumerable<BaseEntity> entities);
    }
}