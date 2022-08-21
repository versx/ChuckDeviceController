﻿namespace ChuckDeviceController.Services
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;

    public class ClearFortsService : IClearFortsService
    {
        #region Variables

        private readonly ILogger<IClearFortsService> _logger;
        private readonly IConfiguration _configuration;
        //private readonly IDbContextFactory<MapContext> _factory;
        private MapContext _context;
        private readonly string _connectionString;

        private readonly Dictionary<ulong, List<string>> _gymIdsPerCell = new();
        private readonly Dictionary<ulong, List<string>> _stopIdsPerCell = new();

        private readonly object _gymCellLock = new();
        private readonly object _stopCellLock = new();

        #endregion

        public ClearFortsService(
            ILogger<IClearFortsService> logger,
            IConfiguration configuration)
            //IDbContextFactory<MapContext> factory)
        {
            _logger = logger;
            _configuration = configuration;
            //_factory = factory;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _context = DbContextFactory.CreateMapDataContext(_connectionString);
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

        public async Task ClearOldFortsAsync()
        {
            var gymsToDelete = new List<Gym>();
            var stopsToDelete = new List<Pokestop>();

            _context = CreateDbContextIfNull();

            //using (var context = _factory.CreateDbContext())
            {
                foreach (var (cellId, pokestopIds) in _stopIdsPerCell)
                {
                    // Get pokestops within S2 cell and not marked deleted
                    var pokestops = _context.Pokestops.Where(stop => stop.CellId == cellId && !stop.IsDeleted)
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

                foreach (var (cellId, gymIds) in _gymIdsPerCell)
                {
                    // Get gyms within S2 cell and not marked deleted
                    var gyms = _context.Gyms.Where(gym => gym.CellId == cellId && !gym.IsDeleted)
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
                    await _context.Pokestops.BulkMergeAsync(stopsToDelete);
                }
                if (gymsToDelete.Count > 0)
                {
                    _logger.LogInformation($"Marking {gymsToDelete.Count:N0} Gyms as deleted since they seem to no longer exist.");
                    await _context.Gyms.BulkMergeAsync(gymsToDelete);
                }
            }
        }

        private MapContext CreateDbContextIfNull()
        {
            if (_context == null)
            {
                _context = DbContextFactory.CreateMapDataContext(_connectionString);
            }
            return _context;
        }
    }
}