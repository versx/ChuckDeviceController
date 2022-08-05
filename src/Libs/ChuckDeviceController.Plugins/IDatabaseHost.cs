namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common.Data.Contracts;

    // TODO: Allow fetching database entities from plugins

    /// <summary>
    /// 
    /// </summary>
    public interface IDatabaseHost
    {
        Task<IReadOnlyList<T>> GetListAsync<T>() where T : IBaseEntity;

        Task<T> GetByIdAsync<T, TId>(TId id) where T : IBaseEntity;
    }
}