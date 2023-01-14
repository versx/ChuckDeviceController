﻿namespace ChuckDeviceConfigurator.Services.Plugins.Hosts;

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

    #endregion

    #region Constructor

    public DatabaseHost(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    #endregion

    #region Public Methods

    public async Task<TEntity?> FindAsync<TEntity, TKey>(TKey id)
        where TKey : notnull
        where TEntity : class
    {
        if (_controllerEntityTypes.Contains(typeof(TEntity)))
        {
            using var context = DbContextFactory.CreateControllerContext(_connectionString);

            if (typeof(TEntity) == typeof(IAccount))
                return await context.Accounts.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IAssignment))
                return await context.Assignments.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IAssignmentGroup))
                return await context.AssignmentGroups.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IDevice))
                return await context.Devices.FindAsync(id.ToString()) as TEntity;
            else if (typeof(TEntity) == typeof(IDeviceGroup))
                return await context.DeviceGroups.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGeofence))
                return await context.Geofences.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IInstance))
                return await context.Instances.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IIvList))
                return await context.IvLists.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IWebhook))
                return await context.Webhooks.FindAsync(id) as TEntity;
        }
        else if (_mapEntityTypes.Contains(typeof(TEntity)))
        {
            using var context = DbContextFactory.CreateMapDataContext(_connectionString);

            if (typeof(TEntity) == typeof(ICell))
                return await context.Cells.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGym))
                return await context.Gyms.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGymDefender))
                return await context.GymDefenders.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IGymTrainer))
                return await context.GymTrainers.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IIncident))
                return await context.Incidents.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IPokemon))
                return await context.Pokemon.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IPokestop))
                return await context.Pokestops.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(ISpawnpoint))
                return await context.Spawnpoints.FindAsync(id) as TEntity;
            else if (typeof(TEntity) == typeof(IWeather))
                return await context.Weather.FindAsync(id) as TEntity;
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
                return await context.Accounts.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IAssignment))
                return await context.Assignments.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IAssignmentGroup))
                return await context.AssignmentGroups.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IDevice))
                return await context.Devices.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IDeviceGroup))
                return await context.DeviceGroups.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGeofence))
                return await context.Geofences.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IInstance))
                return await context.Instances.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IIvList))
                return await context.IvLists.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IWebhook))
                return await context.Webhooks.ToListAsync() as IReadOnlyList<TEntity>;
        }
        else if (_mapEntityTypes.Contains(typeof(TEntity)))
        {
            using var context = DbContextFactory.CreateMapDataContext(_connectionString);

            if (typeof(TEntity) == typeof(ICell))
                return await context.Cells.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGym))
                return await context.Gyms.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGymDefender))
                return await context.GymDefenders.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IGymTrainer))
                return await context.GymTrainers.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IIncident))
                return await context.Incidents.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IPokemon))
                return await context.Pokemon.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IPokestop))
                return await context.Pokestops.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(ISpawnpoint))
                return await context.Spawnpoints.ToListAsync() as IReadOnlyList<TEntity>;
            else if (typeof(TEntity) == typeof(IWeather))
                return await context.Weather.ToListAsync() as IReadOnlyList<TEntity>;
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