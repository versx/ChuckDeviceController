namespace ChuckDeviceConfigurator.Services.Plugins.Hosts
{
    using System.Linq;
    using System.Linq.Expressions;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Extensions;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Plugin;

    /// <summary>
    /// Plugin host handler class used to interact with the database entities.
    /// </summary>
    public class DatabaseHost : IDatabaseHost
    {
        #region Variables

        private static readonly ILogger<IDatabaseHost> _logger =
            new Logger<IDatabaseHost>(LoggerFactory.Create(x => x.AddConsole()));
        private static readonly IReadOnlyList<Type> _controllerEntityTypes = new List<Type>
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
        private static readonly IReadOnlyList<Type> _mapEntityTypes = new List<Type>
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
        private readonly string _connectionString;

        #endregion

        //public IRepository<IAccount, string> Accounts { get; set; }

        //public IRepository<IDevice, string> Devices { get; }

        #region Constructor

        public DatabaseHost(string connectionString)
        {
            _connectionString = connectionString;

            //Accounts = new ControllerEntityRepository<IAccount, string>(deviceFactory);
            //Devices = new ControllerEntityRepository<IDevice, string>(deviceFactory);
        }

        #endregion

        #region Public Methods

        public async Task<TEntity?> FindAsync<TEntity, TKey>(TKey id)
        {
            if (_controllerEntityTypes.Contains(typeof(TEntity)))
            {
                using (var context = DbContextFactory.CreateControllerContext(_connectionString))
                {
                    if (typeof(TEntity) == typeof(IAccount))
                        return (TEntity?)(await context.Accounts.FindAsync(id) as IAccount);
                    else if (typeof(TEntity) == typeof(IAssignment))
                        return (TEntity?)(await context.Assignments.FindAsync(id) as IAssignment);
                    else if (typeof(TEntity) == typeof(IAssignmentGroup))
                        return (TEntity?)(await context.AssignmentGroups.FindAsync(id) as IAssignmentGroup);
                    else if (typeof(TEntity) == typeof(IDevice))
                        return (TEntity?)(await context.Devices.FindAsync(id) as IDevice);
                    else if (typeof(TEntity) == typeof(IDeviceGroup))
                        return (TEntity?)(await context.DeviceGroups.FindAsync(id) as IDeviceGroup);
                    else if (typeof(TEntity) == typeof(IGeofence))
                        return (TEntity?)(await context.Geofences.FindAsync(id) as IGeofence);
                    else if (typeof(TEntity) == typeof(IInstance))
                        return (TEntity?)(await context.Instances.FindAsync(id) as IInstance);
                    else if (typeof(TEntity) == typeof(IIvList))
                        return (TEntity?)(await context.IvLists.FindAsync(id) as IIvList);
                    else if (typeof(TEntity) == typeof(IWebhook))
                        return (TEntity?)(await context.Webhooks.FindAsync(id) as IWebhook);
                }
            }
            else if (_mapEntityTypes.Contains(typeof(TEntity)))
            {
                using (var context = DbContextFactory.CreateMapDataContext(_connectionString))
                {
                    if (typeof(TEntity) == typeof(ICell))
                        return (TEntity?)(await context.Cells.FindAsync(id) as ICell);
                    else if (typeof(TEntity) == typeof(IGym))
                        return (TEntity?)(await context.Gyms.FindAsync(id) as IGym);
                    else if (typeof(TEntity) == typeof(IGymDefender))
                        return (TEntity?)(await context.GymDefenders.FindAsync(id) as IGymDefender);
                    else if (typeof(TEntity) == typeof(IGymTrainer))
                        return (TEntity?)(await context.GymTrainers.FindAsync(id) as IGymTrainer);
                    else if (typeof(TEntity) == typeof(IIncident))
                        return (TEntity?)(await context.Incidents.FindAsync(id) as IIncident);
                    else if (typeof(TEntity) == typeof(IPokemon))
                        return (TEntity?)(await context.Pokemon.FindAsync(id) as IPokemon);
                    else if (typeof(TEntity) == typeof(IPokestop))
                        return (TEntity?)(await context.Pokestops.FindAsync(id) as IPokestop);
                    else if (typeof(TEntity) == typeof(ISpawnpoint))
                        return (TEntity?)(await context.Spawnpoints.FindAsync(id) as ISpawnpoint);
                    else if (typeof(TEntity) == typeof(IWeather))
                        return (TEntity?)(await context.Weather.FindAsync(id) as IWeather);
                }
            }

            _logger.LogError($"Failed to determine DbSet from provided type '{typeof(TEntity).Name}'");
            return default;
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>()
        {
            if (_controllerEntityTypes.Contains(typeof(TEntity)))
            {
                using (var context = DbContextFactory.CreateControllerContext(_connectionString))
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
            }
            else if (_mapEntityTypes.Contains(typeof(TEntity)))
            {
                using (var context = DbContextFactory.CreateMapDataContext(_connectionString))
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
            }

            _logger.LogError($"Failed to determine DbSet from provided type '{typeof(TEntity).Name}'");
            return null;
        }

        // TODO: Refactor method(s)
        public async Task<IReadOnlyList<TEntity>> FindAsync<TEntity, TKey>(
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TKey>>? order = null,
            SortOrderDirection sortDirection = SortOrderDirection.Asc,
            int limit = 1000)
            where TEntity : class
            where TKey : notnull
        {
            List<TEntity>? results = null;
            IQueryable<TEntity>? filtered = null;
            IOrderedQueryable<TEntity>? ordered = null;

            if (_controllerEntityTypes.Contains(typeof(TEntity)))
            {
                using var context = DbContextFactory.CreateControllerContext(_connectionString);
                if (typeof(TEntity) == typeof(IAccount))
                {
                    filtered = (IQueryable<TEntity>)context.Accounts
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IAccount, bool>>);
                }
                else if (typeof(TEntity) == typeof(IAssignment))
                {
                    filtered = (IQueryable<TEntity>)context.Assignments
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IAssignment, bool>>);
                }
                else if (typeof(TEntity) == typeof(IAssignmentGroup))
                {
                    filtered = (IQueryable<TEntity>)context.AssignmentGroups
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IAssignmentGroup, bool>>);
                }
                else if (typeof(TEntity) == typeof(IDevice))
                {
                    filtered = (IQueryable<TEntity>)context.Devices
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IDevice, bool>>);
                }
                else if (typeof(TEntity) == typeof(IDeviceGroup))
                {
                    filtered = (IQueryable<TEntity>)context.DeviceGroups
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IDeviceGroup, bool>>);
                }
                else if (typeof(TEntity) == typeof(IGeofence))
                {
                    filtered = (IQueryable<TEntity>)context.Geofences
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IGeofence, bool>>);
                }
                else if (typeof(TEntity) == typeof(IInstance))
                {
                    filtered = (IQueryable<TEntity>)context.Instances
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IInstance, bool>>);
                }
                else if (typeof(TEntity) == typeof(IIvList))
                {
                    filtered = (IQueryable<TEntity>)context.IvLists
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IIvList, bool>>);
                }
                else if (typeof(TEntity) == typeof(IWebhook))
                {
                    filtered = (IQueryable<TEntity>)context.Webhooks
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IWebhook, bool>>);
                }

                ordered = filtered.OrderBy(order, sortDirection);
                results = await (ordered ?? filtered).ToListAsync();
            }
            else if (_mapEntityTypes.Contains(typeof(TEntity)))
            {
                using var context = DbContextFactory.CreateMapDataContext(_connectionString);
                if (typeof(TEntity) == typeof(ICell))
                {
                    filtered = (IQueryable<TEntity>)context.Cells
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<ICell, bool>>);
                }
                else if (typeof(TEntity) == typeof(IGym))
                {
                    filtered = (IQueryable<TEntity>)context.Gyms
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IGym, bool>>);
                }
                else if (typeof(TEntity) == typeof(IGymDefender))
                {
                    filtered = (IQueryable<TEntity>)context.GymDefenders
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IGymDefender, bool>>);
                }
                else if (typeof(TEntity) == typeof(IGymTrainer))
                {
                    filtered = (IQueryable<TEntity>)context.GymTrainers
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IGymTrainer, bool>>);
                }
                else if (typeof(TEntity) == typeof(IIncident))
                {
                    filtered = (IQueryable<TEntity>)context.Incidents
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IIncident, bool>>);
                }
                else if (typeof(TEntity) == typeof(IPokemon))
                {
                    filtered = (IQueryable<TEntity>)context.Pokemon
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IPokemon, bool>>);
                }
                else if (typeof(TEntity) == typeof(IPokestop))
                {
                    filtered = (IQueryable<TEntity>)context.Pokestops
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IPokestop, bool>>);
                }
                else if (typeof(TEntity) == typeof(ISpawnpoint))
                {
                    filtered = (IQueryable<TEntity>)context.Spawnpoints
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<ISpawnpoint, bool>>);
                }
                else if (typeof(TEntity) == typeof(IWeather))
                {
                    filtered = (IQueryable<TEntity>)context.Weather
                        .AsQueryable()
                        .AsNoTracking()
                        .FilterBy(predicate as Expression<Func<IWeather, bool>>);
                }

                ordered = filtered.OrderBy(order, sortDirection);
                results = await (ordered ?? filtered).ToListAsync();
            }
            return results?.Take(limit).ToList();
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