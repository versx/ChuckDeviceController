namespace ChuckDeviceController.HostedServices;

using System.Collections.Concurrent;
using System.Threading;

using Microsoft.Extensions.Options;
using MySqlConnector;

using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;

public class ClearPokestopsCache : ClearFortsCache
{
    public void AddPokestop(ulong cellId, string pokestopId)
    {
        AddOrUpdate(cellId, new ConcurrentBag<string> { pokestopId }, (key, oldValue) =>
        {
            if (!oldValue.Contains(pokestopId))
            {
                oldValue.Add(pokestopId);
            }
            return oldValue;
        });
    }
}

public class ClearGymsCache : ClearFortsCache
{
    public void AddGym(ulong cellId, string gymId)
    {
        AddOrUpdate(cellId, new ConcurrentBag<string> { gymId }, (key, oldValue) =>
        {
            if (!oldValue.Contains(gymId))
            {
                oldValue.Add(gymId);
            }
            return oldValue;
        });
    }
}

public class ClearFortsCache : ConcurrentDictionary<ulong, ConcurrentBag<string>>
{
    private const int DefaultConcurrencyLevel = 25;
    private const ushort DefaultCapacity = ushort.MaxValue;

    public ClearFortsCache(int concurrencyLevel = DefaultConcurrencyLevel, int capacity = DefaultCapacity)
        : base(concurrencyLevel, capacity)
    {
    }
}

public class ClearFortsHostedService : TimedHostedService
{
    private const uint TimerIntervalS = 15 * 60; // 15 minutes
    private const string UpdateFortsQueryFormat = "UPDATE {0} SET deleted=0 WHERE id IN ({1})";

    #region Variables

    private readonly ClearGymsCache _gymIdsPerCell;
    private readonly ClearPokestopsCache _stopIdsPerCell;
    private readonly ILogger<ClearFortsHostedService> _logger;

    #endregion

    #region Properties

    public DataProcessorOptionsConfig Options { get; }

    #endregion

    #region Constructor

    public ClearFortsHostedService(
        ILogger<ClearFortsHostedService> logger,
        ClearGymsCache gymIdsPerCell,
        ClearPokestopsCache stopIdsPerCell,
        IOptions<DataProcessorOptionsConfig> options)
        : base(logger, TimerIntervalS)
    {
        _logger = logger;
        _gymIdsPerCell = gymIdsPerCell;
        _stopIdsPerCell = stopIdsPerCell;

        Options = options.Value;
    }

    #endregion

    #region Callback Method

    /// <summary>
    /// Mark upgraded/downgraded forts as deleted that no longer exist.
    /// </summary>
    /// <returns></returns>
    private async Task ClearOldFortsAsync()
    {
        try
        {
            using var connection = await EntityRepository.CreateConnectionAsync($"{nameof(ClearFortsHostedService)}::ClearOldFortsAsync");
            if (connection == null)
            {
                _logger.LogError($"Failed to connect to MySQL database server!");
                return;
            }

            var pokestopIds = _stopIdsPerCell.Values.SelectMany(x => x).ToList();
            var stopsToDelete = await GetDeletableFortsAsync<Pokestop>(connection, pokestopIds);
            if (stopsToDelete.Any())
            {
                _logger.LogInformation($"Marking {stopsToDelete.Count():N0} Pokestops as deleted since they no longer seem to exist.");
                var sql = string.Format(UpdateFortsQueryFormat, "pokestop", string.Join(", ", pokestopIds.Select(id => $"'{id}'")));
                var result = await EntityRepository.ExecuteAsync(connection, sql);
                _logger.LogInformation($"{result:N0} Pokestops have been marked as deleted.");
            }

            var gymIds = _gymIdsPerCell.Values.SelectMany(x => x).ToList();
            var gymsToDelete = await GetDeletableFortsAsync<Gym>(connection, gymIds);
            if (gymsToDelete.Any())
            {
                _logger.LogInformation($"Marking {gymsToDelete.Count():N0} Gyms as deleted since they no longer seem to exist.");
                var sql = string.Format(UpdateFortsQueryFormat, "gym", string.Join(", ", gymIds.Select(id => $"'{id}'")));
                var result = await EntityRepository.ExecuteAsync(connection, sql);
                _logger.LogInformation($"{result:N0} Gyms have been marked as deleted.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while marking old forts as deleted: {ex.Message}");
        }

        // Clear fort caches
        _stopIdsPerCell.Clear();
        _gymIdsPerCell.Clear();
    }

    #endregion

    #region BackgroundService Impl

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.CompletedTask;
    }

    protected override async Task RunJobAsync(CancellationToken stoppingToken)
    {
        if (!Options.ClearOldForts)
            return;

        //await ClearOldFortsAsync();
        new Thread(async () => await ClearOldFortsAsync()) { IsBackground = true }.Start();
        await Task.CompletedTask;
    }

    #endregion

    private static async Task<IEnumerable<TEntity>> GetDeletableFortsAsync<TEntity>(MySqlConnection connection, IEnumerable<string> fortIds)
        where TEntity : BaseFort
    {
        // Get forts within S2 cell and not marked deleted. Filter forts that
        // have not been seen within S2 cells by devices.
        var fortsToDelete = new List<TEntity>();
        if (!fortIds.Any())
        {
            return fortsToDelete;
        }

        var stopIdsArg = string.Join(", ", fortIds.Select(x => $"'{x}'"));
        var whereClause = $" WHERE id IN ({stopIdsArg}) AND deleted = false";
        var forts = await EntityRepository.GetEntitiesAsync<string, TEntity>(connection, whereClause);
        if (forts == null)
        {
            return fortsToDelete;
        }

        foreach (var fort in forts)
        {
            fort.IsDeleted = true;
            fortsToDelete.Add(fort);
        }
        return fortsToDelete;
    }
}