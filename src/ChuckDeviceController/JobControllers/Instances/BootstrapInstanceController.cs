namespace ChuckDeviceController.JobControllers.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.JobControllers;
    using Chuck.Infrastructure.JobControllers.Tasks;
    using ChuckDeviceController.Services.Routes;
    using Geofence = Chuck.Infrastructure.Geofence.Models.Geofence;

    public class BootstrapInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<BootstrapInstanceController> _logger;

        private readonly IReadOnlyList<Geofence> _geofences;
        private readonly RouteGenerator _routeGenerator;
        //private static readonly Random _random = new Random();
        //private DateTime _lastCompletedTime;
        private int _lastIndex;
        private readonly object _indexLock = new object();

        #endregion

        #region Properties

        public string Name { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public IReadOnlyList<Coordinate> Coordinates { get; }

        public bool FastBootstrapMode { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        #endregion

        #region Constructor

        public BootstrapInstanceController(string name, List<List<Coordinate>> geofences, ushort minLevel, ushort maxLevel, ushort circleSize = 70, bool fastBootstrapMode = false, string groupName = null, bool isEvent = false)
        {
            Name = name;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            FastBootstrapMode = fastBootstrapMode;
            GroupName = groupName;
            IsEvent = isEvent;

            _logger = new Logger<BootstrapInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _geofences = Geofence.FromPolygons(geofences);
            _routeGenerator = new RouteGenerator();
            Coordinates = _routeGenerator.GenerateBootstrapRoute((List<Geofence>)_geofences, circleSize);
        }

        #endregion

        public async Task<ITask> GetTask(string uuid, string accountUsername, bool startup)
        {
            // TODO: Save last index to Instance.Data
            Coordinate result;
            lock (_indexLock)
            {
                var currentIndex = _lastIndex;
                _logger.LogDebug($"[{uuid}] Current index: {currentIndex}");
                var currentCoord = Coordinates[currentIndex];
                if (!startup)
                {
                    if (_lastIndex + 1 == Coordinates.Count)
                    {
                        //_lastCompletedTime = DateTime.UtcNow;
                        Reload();
                        // TODO: Assign instance to chained instance upon completion?
                    }
                    else
                    {
                        _lastIndex++;
                    }
                }
                result = currentCoord;
            }
            return await Task.FromResult(new BootstrapTask
            {
                Area = Name,
                Action = FastBootstrapMode
                    ? ActionType.ScanRaid
                    : ActionType.ScanPokemon,
                Latitude = result.Latitude,
                Longitude = result.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            }).ConfigureAwait(false);
        }

        public async Task<string> GetStatus()
        {
            var percentage = Math.Round(((double)_lastIndex / (double)Coordinates.Count) * 100.00, 2);
            var text = $"{_lastIndex:N0}/{Coordinates.Count:N0} ({percentage}%)";
            return await Task.FromResult(text).ConfigureAwait(false);
        }

        public void Reload()
        {
            _lastIndex = 0;
            //_lastCompletedTime = default;
        }

        public void Stop()
        {
        }
    }
}