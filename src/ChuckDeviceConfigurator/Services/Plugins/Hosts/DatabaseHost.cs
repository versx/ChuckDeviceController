namespace ChuckDeviceConfigurator.Services.Plugins.Hosts;

using System.Linq;
using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceConfigurator.Extensions;
using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Factories;
using ChuckDeviceController.Data.Repositories;
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
    private readonly IUnitOfWork _uow;

    #endregion

    #region Constructor

    public DatabaseHost(
        IConfiguration configuration,
        IUnitOfWork uow)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        _uow = uow;
    }

    #endregion

    #region Public Methods

    public async Task<TEntity?> FindAsync<TEntity, TKey>(TKey id)
        where TKey : notnull
        where TEntity : class
    {
        if (_controllerEntityTypes.Contains(typeof(TEntity)))
        {
            if (typeof(TEntity) == typeof(IAccount))
                return await _uow.Accounts.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IAssignment))
                return await _uow.Assignments.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IAssignmentGroup))
                return await _uow.AssignmentGroups.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IDevice))
                return await _uow.Devices.FindByIdAsync(id.ToString()) as TEntity;
            else if (typeof(TEntity) == typeof(IDeviceGroup))
                return await _uow.DeviceGroups.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGeofence))
                return await _uow.Geofences.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IInstance))
                return await _uow.Instances.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IIvList))
                return await _uow.IvLists.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IWebhook))
                return await _uow.Webhooks.FindByIdAsync(id) as TEntity;
        }
        else if (_mapEntityTypes.Contains(typeof(TEntity)))
        {
            if (typeof(TEntity) == typeof(ICell))
                return await _uow.Cells.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGym))
                return await _uow.Gyms.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGymDefender))
                return await _uow.GymDefenders.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGymTrainer))
                return await _uow.GymTrainers.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IIncident))
                return await _uow.Incidents.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IPokemon))
                return await _uow.Pokemon.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IPokestop))
                return await _uow.Pokestops.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(ISpawnpoint))
                return await _uow.Spawnpoints.FindByIdAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IWeather))
                return await _uow.Weather.FindByIdAsync(id) as TEntity;
        }

        _logger.LogError($"Failed to determine DbSet from provided type '{typeof(TEntity).Name}'");
        return default;
    }

    public async Task<IReadOnlyList<TEntity>> FindAllAsync<TEntity>()
        where TEntity : class
    {
        if (_controllerEntityTypes.Contains(typeof(TEntity)))
        {
            using var context = DbContextFactory.CreateControllerContext(_connectionString);

            if (typeof(TEntity) == typeof(IAccount))
                return await _uow.Accounts.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IAssignment))
                return await _uow.Assignments.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IAssignmentGroup))
                return await _uow.AssignmentGroups.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IDevice))
                return await _uow.Devices.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IDeviceGroup))
                return await _uow.DeviceGroups.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGeofence))
                return await _uow.Geofences.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IInstance))
                return await _uow.Instances.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IIvList))
                return await _uow.IvLists.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IWebhook))
                return await _uow.Webhooks.FindAllAsync() as IReadOnlyList<TEntity>;
        }
        else if (_mapEntityTypes.Contains(typeof(TEntity)))
        {
            if (typeof(TEntity) == typeof(ICell))
                return await _uow.Cells.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGym))
                return await _uow.Gyms.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGymDefender))
                return await _uow.GymDefenders.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGymTrainer))
                return await _uow.GymTrainers.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IIncident))
                return await _uow.Incidents.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IPokemon))
                return await _uow.Pokemon.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IPokestop))
                return await _uow.Pokestops.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(ISpawnpoint))
                return await _uow.Spawnpoints.FindAllAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IWeather))
                return await _uow.Weather.FindAllAsync() as IReadOnlyList<TEntity>;
        }

        _logger.LogError($"Failed to determine DbSet from provided type '{typeof(TEntity).Name}'");
        return null;
    }

    // REVIEW: Refactor method(s)
    public async Task<IReadOnlyList<TEntity>> FindAsync<TEntity, TKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TKey>>? order = null,
        SortOrderDirection sortDirection = SortOrderDirection.Asc,
        int limit = 1000)
        where TEntity : class
    {
        List<TEntity>? results = null;
        IQueryable<TEntity>? filtered = null;
        IOrderedQueryable<TEntity>? ordered;

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

            if (filtered != null)
            {
                ordered = filtered.Order(order, sortDirection);
                results = await (ordered ?? filtered).ToListAsync();
            }
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

            if (filtered != null)
            {
                ordered = filtered.Order(order, sortDirection);
                results = await (ordered ?? filtered).ToListAsync();
            }
        }
        return results?.Take(limit).ToList();
    }

    #endregion
}