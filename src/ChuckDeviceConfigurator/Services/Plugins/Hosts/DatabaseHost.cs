namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Plugins;

    /// <summary>
    /// Plugin host handler class used to interact with the database entities.
    /// </summary>
    public class DatabaseHost : IDatabaseHost
    {
        #region Variables

        private readonly ILogger<IDatabaseHost> _logger;
        private readonly string _connectionString;
        private readonly IReadOnlyList<Type> _controllerEntityTypes = new List<Type>
        {
            typeof(IAccount),
            typeof(IAssignment),
            typeof(IAssignmentGroup),
            typeof(IDevice),
            typeof(IDeviceGroup),
            typeof(IGeofence),
            typeof(IInstance),
            typeof(IIvList),
            typeof(IWebhook),
        };
        private readonly IReadOnlyList<Type> _mapEntityTypes = new List<Type>
        {
            typeof(ICell),
            typeof(IGym),
            typeof(IGymDefender),
            typeof(IGymTrainer),
            typeof(IIncident),
            typeof(IPokemon),
            typeof(IPokestop),
            typeof(ISpawnpoint),
            typeof(IWeather),
        };

        #endregion

        //public IRepository<IAccount, string> Accounts { get; set; }

        //public IRepository<IDevice, string> Devices { get; }

        #region Constructor

        public DatabaseHost(
            ILogger<IDatabaseHost> logger,
            string connectionString)
        {
            _logger = logger;
            _connectionString = connectionString;

            //Accounts = new ControllerEntityRepository<IAccount, string>(deviceFactory);
            //Devices = new ControllerEntityRepository<IDevice, string>(deviceFactory);
        }

        #endregion

        #region Public Methods

        public async Task<T?> GetByIdAsync<T, TId>(TId id)
        {
            if (_controllerEntityTypes.Contains(typeof(T)))
            {
                using (var context = DbContextFactory.CreateControllerContext(_connectionString))
                {
                    if (typeof(T) == typeof(IAccount))
                        return (T?)(await context.Accounts.FindAsync(id) as IAccount);
                    else if (typeof(T) == typeof(IAssignment))
                        return (T?)(await context.Assignments.FindAsync(id) as IAssignment);
                    else if (typeof(T) == typeof(IAssignmentGroup))
                        return (T?)(await context.AssignmentGroups.FindAsync(id) as IAssignmentGroup);
                    else if (typeof(T) == typeof(IDevice))
                        return (T?)(await context.Devices.FindAsync(id) as IDevice);
                    else if (typeof(T) == typeof(IDeviceGroup))
                        return (T?)(await context.DeviceGroups.FindAsync(id) as IDeviceGroup);
                    else if (typeof(T) == typeof(IGeofence))
                        return (T?)(await context.Geofences.FindAsync(id) as IGeofence);
                    else if (typeof(T) == typeof(IInstance))
                        return (T?)(await context.Instances.FindAsync(id) as IInstance);
                    else if (typeof(T) == typeof(IIvList))
                        return (T?)(await context.IvLists.FindAsync(id) as IIvList);
                    else if (typeof(T) == typeof(IWebhook))
                        return (T?)(await context.Webhooks.FindAsync(id) as IWebhook);
                }
            }
            else if (_mapEntityTypes.Contains(typeof(T)))
            {
                using (var context = DbContextFactory.CreateMapDataContext(_connectionString))
                {
                    if (typeof(T) == typeof(ICell))
                        return (T?)(await context.Cells.FindAsync(id) as ICell);
                    else if (typeof(T) == typeof(IGym))
                        return (T?)(await context.Gyms.FindAsync(id) as IGym);
                    else if (typeof(T) == typeof(IGymDefender))
                        return (T?)(await context.GymDefenders.FindAsync(id) as IGymDefender);
                    else if (typeof(T) == typeof(IGymTrainer))
                        return (T?)(await context.GymTrainers.FindAsync(id) as IGymTrainer);
                    else if (typeof(T) == typeof(IIncident))
                        return (T?)(await context.Incidents.FindAsync(id) as IIncident);
                    else if (typeof(T) == typeof(IPokemon))
                        return (T?)(await context.Pokemon.FindAsync(id) as IPokemon);
                    else if (typeof(T) == typeof(IPokestop))
                        return (T?)(await context.Pokestops.FindAsync(id) as IPokestop);
                    else if (typeof(T) == typeof(ISpawnpoint))
                        return (T?)(await context.Spawnpoints.FindAsync(id) as ISpawnpoint);
                    else if (typeof(T) == typeof(IWeather))
                        return (T?)(await context.Weather.FindAsync(id) as IWeather);
                }
            }

            _logger.LogError($"Failed to determine DbSet from provided type '{typeof(T).Name}'");
            return default;
        }

        public async Task<IReadOnlyList<T>> GetListAsync<T>()
        {
            if (_controllerEntityTypes.Contains(typeof(T)))
            {
                using (var context = DbContextFactory.CreateControllerContext(_connectionString))
                {
                    if (typeof(T) == typeof(IAccount))
                        return (IReadOnlyList<T>)await context.Accounts.ToListAsync();
                    else if (typeof(T) == typeof(IAssignment))
                        return (IReadOnlyList<T>)await context.Assignments.ToListAsync();
                    else if (typeof(T) == typeof(IAssignmentGroup))
                        return (IReadOnlyList<T>)await context.AssignmentGroups.ToListAsync();
                    else if (typeof(T) == typeof(IDevice))
                        return (IReadOnlyList<T>)await context.Devices.ToListAsync();
                    else if (typeof(T) == typeof(IDeviceGroup))
                        return (IReadOnlyList<T>)await context.DeviceGroups.ToListAsync();
                    else if (typeof(T) == typeof(IGeofence))
                        return (IReadOnlyList<T>)await context.Geofences.ToListAsync();
                    else if (typeof(T) == typeof(IInstance))
                        return (IReadOnlyList<T>)await context.Instances.ToListAsync();
                    else if (typeof(T) == typeof(IIvList))
                        return (IReadOnlyList<T>)await context.IvLists.ToListAsync();
                    else if (typeof(T) == typeof(IWebhook))
                        return (IReadOnlyList<T>)await context.Webhooks.ToListAsync();
                }
            }
            else if (_mapEntityTypes.Contains(typeof(T)))
            {
                using (var context = DbContextFactory.CreateMapDataContext(_connectionString))
                {
                    if (typeof(T) == typeof(ICell))
                        return (IReadOnlyList<T>)await context.Cells.ToListAsync();
                    else if (typeof(T) == typeof(IGym))
                        return (IReadOnlyList<T>)await context.Gyms.ToListAsync();
                    else if (typeof(T) == typeof(IGymDefender))
                        return (IReadOnlyList<T>)await context.GymDefenders.ToListAsync();
                    else if (typeof(T) == typeof(IGymTrainer))
                        return (IReadOnlyList<T>)await context.GymTrainers.ToListAsync();
                    else if (typeof(T) == typeof(IIncident))
                        return (IReadOnlyList<T>)await context.Incidents.ToListAsync();
                    else if (typeof(T) == typeof(IPokemon))
                        return (IReadOnlyList<T>)await context.Pokemon.ToListAsync();
                    else if (typeof(T) == typeof(IPokestop))
                        return (IReadOnlyList<T>)await context.Pokestops.ToListAsync();
                    else if (typeof(T) == typeof(ISpawnpoint))
                        return (IReadOnlyList<T>)await context.Spawnpoints.ToListAsync();
                    else if (typeof(T) == typeof(IWeather))
                        return (IReadOnlyList<T>)await context.Weather.ToListAsync();
                }
            }

            _logger.LogError($"Failed to determine DbSet from provided type '{typeof(T).Name}'");
            return null;
        }

        #endregion
    }

    /*
    public class ControllerEntityRepository<TEntity, TId> : IRepository<TEntity, TId>// where TEntity : class, IBaseEntity // : BaseEntity
    {
        private readonly IDbContextFactory<DeviceControllerContext> _factory;

        public ControllerEntityRepository(IDbContextFactory<DeviceControllerContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<TEntity>> GetListAsync()
        {
            using (var context = _factory.CreateDbContext())
            {
                if (typeof(TEntity) == typeof(IAccount))
                    return (IReadOnlyList<TEntity>)await context.Accounts.ToListAsync();
                else if (typeof(TEntity) == typeof(IAssignment))
                    return (IReadOnlyList<TEntity>)await context.Assignments.ToListAsync();
                else if (typeof(TEntity) == typeof(IAssignmentGroup))
                    return (IReadOnlyList<TEntity>)await context.AssignmentGroups.ToListAsync();
                else if (typeof(TEntity) == typeof(IDevice))
                    return (IReadOnlyList<TEntity>)await context.Devices.ToListAsync();
                else if (typeof(TEntity) == typeof(IDeviceGroup))
                    return (IReadOnlyList<TEntity>)await context.DeviceGroups.ToListAsync();
                else if (typeof(TEntity) == typeof(IGeofence))
                    return (IReadOnlyList<TEntity>)await context.Geofences.ToListAsync();
                else if (typeof(TEntity) == typeof(IInstance))
                    return (IReadOnlyList<TEntity>)await context.Instances.ToListAsync();
                else if (typeof(TEntity) == typeof(IIvList))
                    return (IReadOnlyList<TEntity>)await context.IvLists.ToListAsync();
                else if (typeof(TEntity) == typeof(IWebhook))
                    return (IReadOnlyList<TEntity>)await context.Webhooks.ToListAsync();
            }
            return null;
        }

        public async Task<TEntity> GetByIdAsync(TId id)
        {
            using (var context = _factory.CreateDbContext())
            {
                if (typeof(TEntity) == typeof(IAccount))
                    return (TEntity)(await context.Accounts.FindAsync(id) as IAccount);
                else if (typeof(TEntity) == typeof(IAssignment))
                    return (TEntity)(await context.Assignments.FindAsync(id) as IAssignment);
                else if (typeof(TEntity) == typeof(IAssignmentGroup))
                    return (TEntity)(await context.AssignmentGroups.FindAsync(id) as IAssignmentGroup);
                else if (typeof(TEntity) == typeof(IDevice))
                    return (TEntity)(await context.Devices.FindAsync(id) as IDevice);
                else if (typeof(TEntity) == typeof(IDeviceGroup))
                    return (TEntity)(await context.DeviceGroups.FindAsync(id) as IDeviceGroup);
                else if (typeof(TEntity) == typeof(IGeofence))
                    return (TEntity)(await context.Geofences.FindAsync(id) as IGeofence);
                else if (typeof(TEntity) == typeof(IInstance))
                    return (TEntity)(await context.Instances.FindAsync(id) as IInstance);
                else if (typeof(TEntity) == typeof(IIvList))
                    return (TEntity)(await context.IvLists.FindAsync(id) as IIvList);
                else if (typeof(TEntity) == typeof(IWebhook))
                    return (TEntity)(await context.Webhooks.FindAsync(id) as IWebhook);
            }
            return default;
        }
    }

    public class MapEntityRepository<TEntity, TId> : IRepository<TEntity, TId>
    {
        private readonly IDbContextFactory<MapDataContext> _factory;

        public MapEntityRepository(IDbContextFactory<MapDataContext> factory)
        {
            _factory = factory;
        }

        public async Task<IReadOnlyList<TEntity>> GetListAsync()
        {
            using (var context = _factory.CreateDbContext())
            {
                if (typeof(TEntity) == typeof(ICell))
                    return (IReadOnlyList<TEntity>)await context.Cells.ToListAsync();
                else if (typeof(TEntity) == typeof(IGym))
                    return (IReadOnlyList<TEntity>)await context.Gyms.ToListAsync();
                else if (typeof(TEntity) == typeof(IGymDefender))
                    return (IReadOnlyList<TEntity>)await context.GymDefenders.ToListAsync();
                else if (typeof(TEntity) == typeof(IGymTrainer))
                    return (IReadOnlyList<TEntity>)await context.GymTrainers.ToListAsync();
                else if (typeof(TEntity) == typeof(IIncident))
                    return (IReadOnlyList<TEntity>)await context.Incidents.ToListAsync();
                else if (typeof(TEntity) == typeof(IPokemon))
                    return (IReadOnlyList<TEntity>)await context.Pokemon.ToListAsync();
                else if (typeof(TEntity) == typeof(IPokestop))
                    return (IReadOnlyList<TEntity>)await context.Pokestops.ToListAsync();
                else if (typeof(TEntity) == typeof(ISpawnpoint))
                    return (IReadOnlyList<TEntity>)await context.Spawnpoints.ToListAsync();
                else if (typeof(TEntity) == typeof(IWeather))
                    return (IReadOnlyList<TEntity>)await context.Weather.ToListAsync();
            }
            return null;
        }

        public async Task<TEntity> GetByIdAsync(TId id)
        {
            using (var context = _factory.CreateDbContext())
            {
                if (typeof(TEntity) == typeof(ICell))
                    return (TEntity)(await context.Cells.FindAsync(id) as ICell);
                else if (typeof(TEntity) == typeof(IGym))
                    return (TEntity)(await context.Gyms.FindAsync(id) as IGym);
                else if (typeof(TEntity) == typeof(IGymDefender))
                    return (TEntity)(await context.GymDefenders.FindAsync(id) as IGymDefender);
                else if (typeof(TEntity) == typeof(IGymTrainer))
                    return (TEntity)(await context.GymTrainers.FindAsync(id) as IGymTrainer);
                else if (typeof(TEntity) == typeof(IIncident))
                    return (TEntity)(await context.Incidents.FindAsync(id) as IIncident);
                else if (typeof(TEntity) == typeof(IPokemon))
                    return (TEntity)(await context.Pokemon.FindAsync(id) as IPokemon);
                else if (typeof(TEntity) == typeof(IPokestop))
                    return (TEntity)(await context.Pokestops.FindAsync(id) as IPokestop);
                else if (typeof(TEntity) == typeof(ISpawnpoint))
                    return (TEntity)(await context.Spawnpoints.FindAsync(id) as ISpawnpoint);
                else if (typeof(TEntity) == typeof(IWeather))
                    return (TEntity)(await context.Weather.FindAsync(id) as IWeather);
            }
            return default;
        }
    }
    */
}