namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Plugins;

    public class DatabaseHost : IDatabaseHost
    {
        private readonly MapDataContext _mapDataContext;
        private readonly DeviceControllerContext _deviceContext;

        public DatabaseHost(MapDataContext mapDataContext, DeviceControllerContext deviceContext)
        {
            _mapDataContext = mapDataContext;
            _deviceContext = deviceContext;
        }

        public Task<T> GetByIdAsync<T, TId>(TId id) where T : IBaseEntity
        {
            Console.WriteLine($"Id: {id}");
            return null;
        }

        public Task<IReadOnlyList<T>> GetListAsync<T>() where T : IBaseEntity
        {
            return null;
        }
    }
}
