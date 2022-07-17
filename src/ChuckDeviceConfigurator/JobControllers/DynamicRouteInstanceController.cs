namespace ChuckDeviceConfigurator.JobControllers
{
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    // TODO: Inherit most from CircleInstanceController
    public class DynamicRouteInstanceController : /*CircleInstanceController */IJobController, IScanNext
    {
        #region Variables

        private static readonly Random _random = new();
        private readonly ILogger<CircleInstanceController> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRouteCalculator _routeCalculator;
        private readonly Dictionary<string, DeviceIndex> _currentUuid = new();
        private uint _lastIndex = 0; // Used for basic leap frog routing
        private double _lastCompletedTime;
        private double _lastLastCompletedTime;

        #endregion

        #region Properties

        public string Name { get; }

        public IReadOnlyList<Coordinate> Coordinates { get; }

        public IReadOnlyList<MultiPolygon> MultiPolygons { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public bool EnableLureEncounters { get; }

        public bool OptimizeDynamicRoute { get; }

        public Queue<Coordinate> ScanNextCoordinates { get; } = new();

        #endregion

        #region Constructor

        public DynamicRouteInstanceController(
            Instance instance,
            List<MultiPolygon> multiPolygons,
            IRouteGenerator routeGenerator,
            IRouteCalculator routeCalculator)
        {
            _logger = new Logger<CircleInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _routeGenerator = routeGenerator;
            _routeCalculator = routeCalculator;

            Name = instance.Name;
            MultiPolygons = multiPolygons;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;
            EnableLureEncounters = instance.Data?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters;
            OptimizeDynamicRoute = instance.Data?.OptimizeDynamicRoute ?? Strings.DefaultOptimizeDynamicRoute;
            Coordinates = GenerateDynamicRoute();
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(GetTaskOptions options)
        {
            // Add device to device list
            AddDevice(options.Uuid);
            Coordinate? currentCoord = null;

            // Check if on demand scanning coordinates list has any to send to workers
            if (ScanNextCoordinates.Count > 0)
            {
                currentCoord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateTask(currentCoord);
                return await Task.FromResult(scanNextTask);
            }

            if (Coordinates.Count == 0)
            {
                // TODO: Throw error that instance requires at least one coordinate
                _logger.LogError($"[{Name}] Instance requires at least one coordinate, please edit it to contain one.");
                return null;
            }

            // Get coordinates
            currentCoord = GetNextLocation(options.Uuid);

            // Check if we were unable to retrieve a coordinate to send
            if (currentCoord == null)
            {
                // TODO: Not sure if this will ever hit, need to test
                return null;
            }

            var task = CreateTask(currentCoord);
            return await Task.FromResult(task);
        }

        public async Task<string> GetStatusAsync()
        {
            var status = "--";
            if (_lastCompletedTime > 0 && _lastLastCompletedTime > 0)
            {
                var timeDiffSeconds = _lastCompletedTime - _lastLastCompletedTime;
                if (timeDiffSeconds > 0)
                {
                    var time = Math.Round(timeDiffSeconds, 2);
                    status = $"Round Time: {time:N0}s";
                }
            }
            return await Task.FromResult(status);
        }

        public Task Reload()
        {
            _logger.LogDebug($"[{Name}] Reloading instance");

            // TODO: Lock lastIndex
            _lastIndex = 0;
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _logger.LogDebug($"[{Name}] Stopping instance");
            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private ITask CreateTask(Coordinate coord)
        {
            return new CircleTask
            {
                Area = Name,
                Action = DeviceActionType.ScanPokemon,
                Latitude = coord.Latitude,
                Longitude = coord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
                LureEncounter = EnableLureEncounters,
            };
        }

        private Coordinate GetNextLocation(string uuid)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var currentUuidIndex = _currentUuid.ContainsKey(uuid)
                ? _currentUuid[uuid].Index
                : _random.Next(0, Coordinates.Count);

            _currentUuid[uuid] = new DeviceIndex
            {
                Index = currentUuidIndex,
                LastSeen = now,
            };

            var shouldAdvance = true;
            var jumpDistance = 0d;

            if (_currentUuid.Count > 1 && _random.Next(0, 100) < 15)
            {
                (uint numLiveDevices, double distanceToNextDevice) = GetDeviceSpacing(uuid);
                var dist = 10 * distanceToNextDevice * numLiveDevices + 5;
                if (dist < 10 * Coordinates.Count)
                {
                    shouldAdvance = false;
                }
                if (dist > 12 * Coordinates.Count)
                {
                    jumpDistance = distanceToNextDevice - Coordinates.Count / numLiveDevices - 1;
                }
            }
            if (currentUuidIndex == 0 && Coordinates.Count > 1)
            {
                shouldAdvance = true;
            }
            if (shouldAdvance)
            {
                currentUuidIndex += Convert.ToInt32(jumpDistance + 1);
                if (currentUuidIndex >= Coordinates.Count)
                {
                    // Completed route, reset index
                    currentUuidIndex -= Coordinates.Count;
                    _lastLastCompletedTime = _lastCompletedTime;
                    _lastCompletedTime = now;
                }
            }
            else
            {
                currentUuidIndex--;
                if (currentUuidIndex < 0)
                {
                    currentUuidIndex = Coordinates.Count;
                }
            }
            _currentUuid[uuid].Index = currentUuidIndex;
            var currentCoord = Coordinates[currentUuidIndex];
            return currentCoord;
        }

        private double GetRouteDistance(double x, double y)
        {
            return x < y
                ? y - x
                : y + (Coordinates.Count - x);
        }

        private (uint, double) GetDeviceSpacing(string uuid)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var deadDeviceCutOffTime = now - 60;
            var liveDevices = new List<string>();

            // Sort indexes by current device index
            var uuidIndexes = _currentUuid.ToList();
            uuidIndexes.Sort((pair1, pair2) => pair1.Value.Index.CompareTo(pair2.Value.Index));
            var dict = uuidIndexes.ToDictionary(key => key.Key, value => value.Value);

            foreach (var (currentUuid, index) in dict)
            {
                if (index.LastSeen > 0)
                {
                    if (index.LastSeen < deadDeviceCutOffTime)
                    {
                        // Device has not updated in the last 60 seconds, assume dead/offline
                        _currentUuid[currentUuid] = null;
                    }
                    else
                    {
                        liveDevices.Add(currentUuid);
                    }
                }
            }

            var numLiveDevices = (uint)liveDevices.Count;
            double distanceToNextDevice = Coordinates.Count;
            for (var i = 0; i < numLiveDevices; i++)
            {
                if (uuid != liveDevices[i])
                    continue;

                var currentUuid = _currentUuid[uuid];
                if (i < numLiveDevices - 1)
                {
                    var nextDevice = liveDevices[i + 1];
                    var nextUuid = _currentUuid[nextDevice];
                    distanceToNextDevice = GetRouteDistance(currentUuid.Index, nextUuid.Index);
                }
                else
                {
                    var nextDevice = liveDevices[i]; // TODO: Check, was `0`
                    var nextUuid = _currentUuid[nextDevice];
                    distanceToNextDevice = GetRouteDistance(currentUuid.Index, nextUuid.Index);
                }
            }
            return (numLiveDevices, distanceToNextDevice);
        }

        private void AddDevice(string uuid)
        {
            // Check if device already exists
            if (_currentUuid.ContainsKey(uuid))
                return;

            _currentUuid.Add(uuid, new DeviceIndex
            {
                LastSeen = DateTime.UtcNow.ToTotalSeconds(),
            });
        }

        private List<Coordinate> GenerateDynamicRoute()
        {
            var route = _routeGenerator.GenerateRoute(new RouteGeneratorOptions
            {
                CircleSize = Strings.DefaultCircleSize,
                RouteType = RouteGenerationType.Randomized,
                MultiPolygons = (List<MultiPolygon>)MultiPolygons,
                MaximumPoints = 500,
            });

            if (OptimizeDynamicRoute)
            {
                _routeCalculator.ClearCoordinates();
                _routeCalculator.AddCoordinates(route);
                var optimized = _routeCalculator.CalculateShortestRoute();
                _routeCalculator.ClearCoordinates();
                return optimized.ToList();
            }
            return route;
        }

        #endregion

        private class DeviceIndex
        {
            public int Index { get; set; }

            public ulong LastSeen { get; set; }
        }
    }
}