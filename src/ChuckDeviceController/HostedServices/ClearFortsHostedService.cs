namespace ChuckDeviceController.HostedServices
{
    using System.Threading;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;

    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class ClearFortsHostedService : TimedHostedService, IClearFortsHostedService
    {
        #region Variables

        private readonly ILogger<IClearFortsHostedService> _logger;
        private readonly IDbContextFactory<MapDbContext> _factory;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell = new();
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell = new();

        private readonly object _gymCellLock = new();
        private readonly object _stopCellLock = new();

        #endregion

        #region Properties

        public DataProcessorOptionsConfig Options { get; }

        public override uint TimerIntervalS => 15 * 60; // 15 minutes

        #endregion

        #region Constructor

        public ClearFortsHostedService(
            ILogger<IClearFortsHostedService> logger,
            IDbContextFactory<MapDbContext> factory,
            IOptions<DataProcessorOptionsConfig> options)
        {
            _logger = logger;
            _factory = factory;

            Options = options.Value;
        }

        #endregion

        #region Add Cells

        public void AddCell(ulong cellId)
        {
            if (!Options.ClearOldForts)
                return;

            lock (_gymCellLock)
            {
                if (!_gymIdsPerCell.ContainsKey(cellId))
                {
                    _gymIdsPerCell.Add(cellId, new());
                }
            }

            lock (_stopCellLock)
            {
                if (!_stopIdsPerCell.ContainsKey(cellId))
                {
                    _stopIdsPerCell.Add(cellId, new());
                }
            }
        }

        public void AddGym(ulong cellId, string gymId)
        {
            if (!Options.ClearOldForts)
                return;

            lock (_gymCellLock)
            {
                if (!_gymIdsPerCell.ContainsKey(cellId))
                {
                    _gymIdsPerCell.Add(cellId, new());
                }
                _gymIdsPerCell[cellId].Add(gymId);
            }
        }

        public void AddPokestop(ulong cellId, string pokestopId)
        {
            if (!Options.ClearOldForts)
                return;

            lock (_stopCellLock)
            {
                if (!_stopIdsPerCell.ContainsKey(cellId))
                {
                    _stopIdsPerCell.Add(cellId, new());
                }
                _stopIdsPerCell[cellId].Add(pokestopId);
            }
        }

        #endregion

        #region Clear Cells

        public void ClearCells()
        {
            //ClearPokestops();
            //ClearGyms();
            lock (_stopCellLock)
            {
                _stopIdsPerCell.Clear();
            }
            lock (_gymCellLock)
            {
                _gymIdsPerCell.Clear();
            }
        }

        public void ClearPokestops()
        {
            lock (_stopCellLock)
            {
                _stopIdsPerCell.Clear();
                //_stopIdsPerCell.Keys.ToList().ForEach(cellId =>
                //{
                //    _stopIdsPerCell[cellId].Clear();
                //});
            }
        }

        public void ClearGyms()
        {
            lock (_gymCellLock)
            {
                _gymIdsPerCell.Clear();
                //_gymIdsPerCell.Keys.ToList().ForEach(cellId =>
                //{
                //    _gymIdsPerCell[cellId].Clear();
                //});
            }
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
                using var context = await _factory.CreateDbContextAsync();

                var stopsToDelete = new List<Pokestop>();
                var stopIdKeys = _stopIdsPerCell.Keys.ToList();
                for (var i = 0; i < stopIdKeys.Count; i++)
                {
                    var cellId = stopIdKeys[i];
                    var pokestopIds = _stopIdsPerCell[cellId];
                    if (!pokestopIds.Any())
                        continue;

                    // Get pokestops within S2 cell and not marked deleted
                    // Filter pokestops that have not been seen within S2 cell by devices
                    var pokestops = context.Pokestops
                        .Where(stop => stop.CellId == cellId && !stop.IsDeleted)
                        .Where(stop => !pokestopIds.Contains(stop.Id))
                        .ToList();
                    if (pokestops.Any())
                    {
                        // Mark gyms as deleted
                        pokestops.ForEach(stop => stop.IsDeleted = true);
                        stopsToDelete.AddRange(pokestops);
                    }
                }

                var gymsToDelete = new List<Gym>();
                var gymIdKeys = _gymIdsPerCell.Keys.ToList();
                for (var i = 0; i < gymIdKeys.Count; i++)
                {
                    var cellId = gymIdKeys[i];
                    var gymIds = _gymIdsPerCell[cellId];
                    if (!gymIds.Any())
                        continue;

                    // Get gyms within S2 cell and not marked deleted
                    // Filter gyms that have not been seen within S2 cell by devices
                    var gyms = context.Gyms
                        .Where(gym => gym.CellId == cellId && !gym.IsDeleted)
                        .Where(gym => !gymIds.Contains(gym.Id))
                        .ToList();
                    if (gyms.Any())
                    {
                        // Mark gyms as deleted
                        gyms.ForEach(gym => gym.IsDeleted = true);
                        gymsToDelete.AddRange(gyms);
                    }
                }

                if (stopsToDelete.Count > 0)
                {
                    _logger.LogInformation($"Marking {stopsToDelete.Count:N0} Pokestops as deleted since they seem to no longer exist.");
                    await context.Pokestops.BulkMergeAsync(stopsToDelete, options =>
                    {
                        options.UseTableLock = true;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.IsDeleted,
                        };
                    });
                }

                if (gymsToDelete.Count > 0)
                {
                    _logger.LogInformation($"Marking {gymsToDelete.Count:N0} Gyms as deleted since they seem to no longer exist.");
                    await context.Gyms.BulkMergeAsync(gymsToDelete, options =>
                    {
                        options.UseTableLock = true;
                        options.OnMergeUpdateInputExpression = p => new
                        {
                            p.IsDeleted,
                        };
                    });
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

            await ClearOldFortsAsync();
        }

        #endregion
    }
}