namespace ChuckDeviceController.HostedServices;

using System.Collections.Concurrent;
using System.Threading;

using Microsoft.Extensions.Options;
using MySqlConnector;

using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories;

// TODO: Remove IClearFortsHostedService contract and register Queue with DI to prevent multiple services
public class ClearFortsHostedService : TimedHostedService, IClearFortsHostedService
{
    private const uint TimerIntervalS = 15 * 60; // 15 minutes
    private const int DefaultConcurrencyLevel = 25;
    private const ushort DefaultCapacity = ushort.MaxValue;

    #region Variables

    private static readonly ConcurrentDictionary<ulong, ConcurrentBag<string>> _gymIdsPerCell = new(DefaultConcurrencyLevel, DefaultCapacity);
    private static readonly ConcurrentDictionary<ulong, ConcurrentBag<string>> _stopIdsPerCell = new(DefaultConcurrencyLevel, DefaultCapacity);
    private readonly ILogger<IClearFortsHostedService> _logger;

    #endregion

    #region Properties

    public DataProcessorOptionsConfig Options { get; }

    #endregion

    #region Constructor

    public ClearFortsHostedService(
        ILogger<IClearFortsHostedService> logger,
        IOptions<DataProcessorOptionsConfig> options)
        : base(logger, TimerIntervalS)
    {
        _logger = logger;

        Options = options.Value;
    }

    #endregion

    #region Add Cells

    public void AddGym(ulong cellId, string gymId)
    {
        if (!Options.ClearOldForts)
            return;

        _gymIdsPerCell.AddOrUpdate(cellId, new ConcurrentBag<string> { gymId }, (key, oldValue) =>
        {
            oldValue.Add(gymId);
            return oldValue;
        });
    }

    public void AddPokestop(ulong cellId, string pokestopId)
    {
        if (!Options.ClearOldForts)
            return;

        _stopIdsPerCell.AddOrUpdate(cellId, new ConcurrentBag<string> { pokestopId }, (key, oldValue) =>
        {
            oldValue.Add(pokestopId);
            return oldValue;
        });
    }

    #endregion

    #region Clear Cells

    public void ClearPokestops()
    {
        _stopIdsPerCell.Clear();
    }

    public void ClearGyms()
    {
        _gymIdsPerCell.Clear();
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
            using var connection = await EntityRepository.CreateConnectionAsync($"{nameof(IClearFortsHostedService)}::ClearOldFortsAsync");
            if (connection == null)
            {
                _logger.LogError($"Failed to connect to MySQL database server!");
                return;
            }

            var stopsToDelete = new List<Pokestop>();
            var stopCellIds = _stopIdsPerCell.Keys
                .Where(x => _stopIdsPerCell[x].Any())
                .ToList();
            for (var i = 0; i < stopCellIds.Count; i++)
            {
                var cellId = stopCellIds[i];
                var pokestopIds = _stopIdsPerCell[cellId];
                if (!pokestopIds.Any())
                    continue;

                //Console.WriteLine($"Checking cell id {cellId} with {pokestopIds.Count:N0} pokestops");
                var pokestops = await GetDeletableFortsAsync<Pokestop>(connection, cellId, pokestopIds);
                if (pokestops.Any())
                {
                    stopsToDelete.AddRange(pokestops);
                }
            }

            var gymsToDelete = new List<Gym>();
            var gymCellIds = _gymIdsPerCell.Keys
                .Where(x => _gymIdsPerCell[x].Any())
                .ToList();
            for (var i = 0; i < gymCellIds.Count; i++)
            {
                var cellId = gymCellIds[i];
                var gymIds = _gymIdsPerCell[cellId];
                if (!gymIds.Any())
                    continue;

                //Console.WriteLine($"Checking cell id {cellId} with {gymIds.Count:N0} gyms");
                var gyms = await GetDeletableFortsAsync<Gym>(connection, cellId, gymIds);
                if (gyms.Any())
                {
                    gymsToDelete.AddRange(gyms);
                }
            }

            if (stopsToDelete.Any())
            {
                _logger.LogInformation($"Marking {stopsToDelete.Count:N0} Pokestops as deleted since they no longer seem to exist.");
                var result = await EntityRepository.ExecuteBulkAsync("pokestop", stopsToDelete, new ColumnDataExpression<Pokestop>
                {
                    { "id", x => x.Id },
                    { "deleted", x => x.IsDeleted },
                });
                _logger.LogInformation($"{result:N0} Pokestops have been marked as deleted.");
            }

            if (gymsToDelete.Any())
            {
                _logger.LogInformation($"Marking {gymsToDelete.Count:N0} Gyms as deleted since they no longer seem to exist.");
                var result = await EntityRepository.ExecuteBulkAsync("gym", gymsToDelete, new ColumnDataExpression<Gym>
                {
                    { "id", x => x.Id },
                    { "deleted", x => x.IsDeleted },
                });
                _logger.LogInformation($"{result:N0} Gyms have been marked as deleted.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while marking old forts as deleted: {ex.Message}");
        }
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

    private static async Task<IEnumerable<TEntity>> GetDeletableFortsAsync<TEntity>(MySqlConnection connection, ulong cellId, IEnumerable<string> fortIds)
        where TEntity : BaseFort
    {
        var fortsToDelete = new List<TEntity>();

        // Get forts within S2 cell and not marked deleted.
        // Filter forts that have not been seen within S2
        // cell by devices.
        var stopIdsArg = string.Join(", ", fortIds.Select(x => $"'{x}'"));
        var whereClause = $" WHERE cell_id = {cellId} AND id NOT IN ({stopIdsArg}) AND deleted = false";
        var forts = await EntityRepository.GetEntitiesAsync<string, TEntity>(connection, whereClause);

        if (forts != null)
        {
            foreach (var fort in forts)
            {
                fort.IsDeleted = true;
                fortsToDelete.Add(fort);
            }
        }
        return fortsToDelete;
    }
}