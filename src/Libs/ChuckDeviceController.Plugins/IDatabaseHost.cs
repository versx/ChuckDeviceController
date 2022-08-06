namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Entities;

    /// <summary>
    /// Plugin host handler contract used to interact with the database entities.
    /// </summary>
    public interface IDatabaseHost
    {
        //IRepository<IAccount, string> Accounts { get; }
        //IRepository<IPokestop, string> Pokestops { get; }
        //IRepository<IDevice, string> Devices { get; }

        Task<IReadOnlyList<T>> GetListAsync<T>();

        Task<T> GetByIdAsync<T, TId>(TId id);
    }

    public interface IRepository<TEntity, TId> //where TEntity : IBaseEntity
    {
        Task<IReadOnlyList<TEntity>> GetListAsync();

        Task<TEntity> GetByIdAsync(TId id);
    }

    public enum DatabaseType
    {
        Controller,
        Map,
    }
}