namespace ChuckDeviceController.Services
{
    using System.Threading;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.HostedServices;

    public class ClearFortsService : TimedHostedService, IClearFortsService
    {
        #region Variables

        private readonly ILogger<IClearFortsService> _logger;
        private readonly IDbContextFactory<MapDbContext> _factory;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell = new();
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell = new();

        private readonly object _gymCellLock = new();
        private readonly object _stopCellLock = new();

        #endregion

        public override uint TimerIntervalMs => 1 * 60 * 1000; // 15 minutes

        public ClearFortsService(
            ILogger<IClearFortsService> logger,
            IDbContextFactory<MapDbContext> factory) : base(new Logger<TimedHostedService>(LoggerFactory.Create(x => x.AddConsole())))
        {
            _logger = logger;
            _factory = factory;
        }

        public void AddCell(ulong cellId)
        {
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
            lock (_stopCellLock)
            {
                if (!_stopIdsPerCell.ContainsKey(cellId))
                {
                    _stopIdsPerCell.Add(cellId, new());
                }
                _stopIdsPerCell[cellId].Add(pokestopId);
            }
        }

        /// <summary>
        /// Mark upgraded/downgraded forts as deleted that no longer exist.
        /// </summary>
        /// <returns></returns>
        public async Task ClearOldFortsAsync()
        {
            // TODO: Fix 'second operation' race condition
            try
            {
                using var context = _factory.CreateDbContext();

                var stopsToDelete = new List<Pokestop>();
                var stopIdKeys = _stopIdsPerCell.Keys.ToList();
                for (var i = 0; i < stopIdKeys.Count; i++)
                {
                    var cellId = stopIdKeys[i];
                    var pokestopIds = _stopIdsPerCell[cellId];

                    // Get pokestops within S2 cell and not marked deleted
                    var pokestops = context.Pokestops.Where(stop => stop.CellId == cellId && !stop.IsDeleted)
                                                     .ToList();
                    if (pokestopIds.Count > 0)
                    {
                        // Filter pokestops that have not been seen within S2 cell by devices
                        pokestops = pokestops.Where(stop => !pokestopIds.Contains(stop.Id))
                                             .ToList();
                    }
                    if (pokestops.Count > 0)
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

                    // Get gyms within S2 cell and not marked deleted
                    var gyms = context.Gyms.Where(gym => gym.CellId == cellId && !gym.IsDeleted)
                                           .ToList();
                    if (gymIds.Count > 0)
                    {
                        // Filter gyms that have not been seen within S2 cell by devices
                        gyms = gyms.Where(gym => !gymIds.Contains(gym.Id))
                                   .ToList();
                    }
                    if (gyms.Count > 0)
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        protected override async Task RunJobAsync(CancellationToken stoppingToken)
        {
            await ClearOldFortsAsync();
        }
    }
}