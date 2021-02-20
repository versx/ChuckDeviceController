namespace ChuckDeviceController.JobControllers.Instances
{
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.JobControllers.Tasks;
    using ChuckDeviceController.Services.Routes;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Geofence = ChuckDeviceController.Geofence.Models.Geofence;

    public class BootstrapInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<BootstrapInstanceController> _logger;

        private readonly IReadOnlyList<Geofence> _geofences;
        private readonly RouteGenerator _routeGenerator;
        //private static readonly Random _random = new Random();
        private DateTime _lastCompletedTime;
        private int _lastIndex;
        private DateTime _lastLastCompletedTime;

        private readonly object _indexLock = new object();

        #endregion

        #region Properties

        public string Name { get; set; }

        public ushort MinimumLevel { get; set; }

        public ushort MaximumLevel { get; set; }

        public IReadOnlyList<Coordinate> Coordinates { get; }

        #endregion

        #region Constructor

        public BootstrapInstanceController(string name, List<List<Coordinate>> geofences, ushort minLevel, ushort maxLevel, ushort circleSize = 70)
        {
            Name = name;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;

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
                int currentIndex = _lastIndex;
                _logger.LogDebug($"[{uuid}] Current index: {currentIndex}");
                Coordinate currentCoord = Coordinates[currentIndex];
                if (!startup)
                {
                    if (_lastIndex + 1 == Coordinates.Count)
                    {
                        _lastLastCompletedTime = _lastCompletedTime;
                        _lastCompletedTime = DateTime.UtcNow;
                        // Wait at last coord, upon complete, _lastIndex = 0;
                        // TODO: Assugn instance to chained instance upon completion?
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
                Latitude = result.Latitude,
                Longitude = result.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            }).ConfigureAwait(false);
        }

        public async Task<string> GetStatus()
        {
            double percentage = Math.Round((_lastIndex / (double)Coordinates.Count) * 100.00, 2);
            string text = $"{_lastIndex:N0}/{Coordinates.Count:N0} ({percentage}%)";
            return await Task.FromResult(text).ConfigureAwait(false);
        }

        public void Reload()
        {
            _lastIndex = 0;
            _lastCompletedTime = default;
            _lastLastCompletedTime = default;
        }

        public void Stop()
        {
        }
    }
}