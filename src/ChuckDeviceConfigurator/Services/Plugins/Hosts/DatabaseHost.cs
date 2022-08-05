namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Plugins;

    public class DatabaseHost : IDatabaseHost
    {
        private readonly ILogger<IDatabaseHost> _logger;
        private readonly IDbContextFactory<DeviceControllerContext> _deviceFactory;
        private readonly IDbContextFactory<MapDataContext> _mapFactory;

        public DatabaseHost(
            ILogger<IDatabaseHost> logger,
            IDbContextFactory<DeviceControllerContext> deviceFactory,
            IDbContextFactory<MapDataContext> mapFactory)
        {
            _logger = logger;
            _deviceFactory = deviceFactory;
            _mapFactory = mapFactory;
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
