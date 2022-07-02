namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public class CircleInstanceController : IJobController
    {
        private static readonly Random _random = new();
        private readonly Dictionary<string, DeviceIndex> _lastUuid = new(); // TODO: Use to replace the two fields below
        private readonly Dictionary<string, int> _currentUuidIndexes = new();
        private readonly Dictionary<string, ulong> _currentUuidSeenTime = new();
        private Queue<Coordinate> _scanNextCoords = new();
        private uint _lastIndex = 0;
        private double _lastCompletedTime;
        private double _lastLastCompletedTime;

        public bool SendTaskForLureEncounter { get; set; } // TODO: Make configurable

        #region Properties

        public string Name { get; }

        public List<Coordinate> Coordinates { get; }

        public CircleInstanceType CircleType { get; }

        public CircleInstanceRouteType RouteType { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string? GroupName { get; set; } // TODO: Fix warning

        public bool IsEvent { get; }

        #endregion

        #region Constructor

        public CircleInstanceController(Instance instance, List<Coordinate> coords, CircleInstanceType circleType)
        {
            Name = instance.Name;
            Coordinates = coords;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            CircleType = circleType;
            RouteType = instance.Data.CircleRouteType;
            GroupName = instance.Data.AccountGroup ?? null;
            IsEvent = instance.Data.IsEvent;
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string accountUsername, bool isStartup)
        {
            var task = GetNextScanLocation(uuid);
            return task;

            // TODO: AddDevice(uuid);
            Coordinate? currentCoord = null;
            switch (CircleType)
            {
                case CircleInstanceType.Pokemon:
                case CircleInstanceType.Raid:
                    switch (RouteType)
                    {
                        case CircleInstanceRouteType.Default:
                            //currentCoord = BasicRoute(uuid, isStartup);
                            break;
                        case CircleInstanceRouteType.Split:
                        case CircleInstanceRouteType.Circular:
                            break;
                    }
                    break;
            }
            if (currentCoord == null)
            {
                // TODO: Not sure if this will ever hit, need to test
                return null;
            }

            return await Task.FromResult(new CircleTask
            {
                Action = CircleType == CircleInstanceType.Pokemon
                    ? DeviceActionType.ScanPokemon
                    : DeviceActionType.ScanRaid,
                Area = Name,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            });
        }

        public async Task<string> GetStatusAsync()
        {
            var status = "--";
            if (_lastCompletedTime != default && _lastLastCompletedTime != default)
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

        public void Reload()
        {
            // TODO: Lock lastIndex
            _lastIndex = 0;
        }

        public void Stop()
        {
        }

        #endregion

        private double GetRouteDistance(double x, double y)
        {
            if (x < y)
            {
                return y - x;
            }
            return y + (Coordinates.Count - x);
        }

        private (int, double) CheckDeviceSpacing(string uuid)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var deadDeviceCutOffTime = now - 60;
            var liveDevices = new List<string>();
            //var indexes = _currentUuidIndexes.sort // TODO: Sort indexes by index

            foreach (var (currentUuid, _) in _currentUuidIndexes)
            {
                if (!_currentUuidSeenTime.ContainsKey(currentUuid))
                {
                    continue;
                }
                var lastSeen = _currentUuidSeenTime[currentUuid];
                if (lastSeen != default)
                {
                    var lastSeenSeconds = lastSeen;//.ToTotalSeconds();
                    if (lastSeenSeconds < deadDeviceCutOffTime)
                    {
                        // TODO: Null
                        _currentUuidIndexes[currentUuid] = default;
                        _currentUuidSeenTime[currentUuid] = default;
                    }
                    else
                    {
                        liveDevices.Add(currentUuid);
                    }
                }
            }

            var numLiveDevices = liveDevices.Count;
            double distanceToNext = Coordinates.Count;
            for (var i = 0; i < numLiveDevices; i++)
            {
                if (uuid != liveDevices[i])
                {
                    continue;
                }
                if (i < numLiveDevices - 1)
                {
                    var nextDevice = liveDevices[i + 1];
                    distanceToNext = GetRouteDistance(_currentUuidIndexes[uuid], _currentUuidIndexes[nextDevice]);
                }
                else
                {
                    var nextDevice = liveDevices[i]; // TODO: Check, was `0`
                    distanceToNext = GetRouteDistance(_currentUuidIndexes[uuid], _currentUuidIndexes[nextDevice]);
                }
            }
            return (numLiveDevices, distanceToNext);
        }

        private CircleTask GetNextScanLocation(string uuid)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var currentIndex = 0;
            var currentUuidIndex = 0;
            // TODO: Ensure Coordinates has at least one coordinate in list
            var currentCoord = Coordinates[currentIndex];
            // TODO: Double check logic
            if (_scanNextCoords.Count > 0)
            {
                currentCoord = _scanNextCoords.Dequeue();
                var task = new CircleTask
                {
                    Action = DeviceActionType.ScanPokemon,
                    Area = Name,
                    Latitude = currentCoord.Latitude,
                    Longitude = currentCoord.Longitude,
                    MinimumLevel = MinimumLevel,
                    MaximumLevel = MaximumLevel,
                    LureEncounter = SendTaskForLureEncounter,
                };
                return task;
            }

            if (CircleType == CircleInstanceType.Pokemon) // TODO: Smart Pokemon
            {
                currentUuidIndex = _currentUuidIndexes.ContainsKey(uuid)
                    ? _currentUuidIndexes[uuid]
                    : _random.Next(0, Coordinates.Count);
                // TODO: Check if uuid exists in _currentUuidIndexes dictionary
                _currentUuidIndexes[uuid] = currentUuidIndex;
                _currentUuidSeenTime[uuid] = now;
                var shouldAdvance = true;
                var jumpDistance = 0d;

                if (_currentUuidIndexes.Count > 1 && _random.Next(0, 100) < 15)
                {
                    var (numLiveDevices, distanceToNext) = CheckDeviceSpacing(uuid);
                    var dist = 10 * distanceToNext * numLiveDevices + 5;
                    if (dist < 10 * Coordinates.Count)
                    {
                        shouldAdvance = false;
                    }
                    if (dist > 12 * Coordinates.Count)
                    {
                        jumpDistance = distanceToNext - Coordinates.Count / numLiveDevices - 1;
                    }
                }
                if (currentUuidIndex == 0 && Coordinates.Count > 1)
                {
                    shouldAdvance = true;
                }
                if (shouldAdvance)
                {
                    currentUuidIndex += Convert.ToInt32(jumpDistance + 1);
                    if (currentUuidIndex >= Coordinates.Count - 1)
                    {
                        currentUuidIndex -= Coordinates.Count - 1;
                        _lastLastCompletedTime = _lastCompletedTime;
                        _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();
                    }
                }
                else
                {
                    currentUuidIndex -= 1;
                    if (currentUuidIndex < 0)
                    {
                        currentUuidIndex = Coordinates.Count - 1;
                    }
                }
                _currentUuidIndexes[uuid] = currentUuidIndex;
                currentCoord = Coordinates[currentUuidIndex];
                var task = new CircleTask
                {
                    Action = DeviceActionType.ScanPokemon,
                    Area = Name,
                    Latitude = currentCoord.Latitude,
                    Longitude = currentCoord.Longitude,
                    MinimumLevel = MinimumLevel,
                    MaximumLevel = MaximumLevel,
                    LureEncounter = SendTaskForLureEncounter,
                };
                return task;
            }
            else
            {
                currentIndex = (int)_lastIndex;
                if (_lastIndex + 1 == Coordinates.Count)
                {
                    _lastLastCompletedTime = _lastCompletedTime;
                    _lastCompletedTime = DateTime.UtcNow.ToTotalSeconds();
                    _lastIndex = 0;
                }
                else
                {
                    _lastIndex++;
                }
                currentCoord = Coordinates[currentIndex];
                var task = new CircleTask
                {
                    Action = CircleType == CircleInstanceType.Pokemon
                        ? DeviceActionType.ScanPokemon
                        : DeviceActionType.ScanRaid,
                    Area = Name,
                    Latitude = currentCoord.Latitude,
                    Longitude = currentCoord.Longitude,
                    MinimumLevel = MinimumLevel,
                    MaximumLevel = MaximumLevel,
                    LureEncounter = SendTaskForLureEncounter,
                };
                return task;
            }
        }
    }

    public class DeviceIndex
    {
        public int Index { get; set; }

        public double LastSeen { get; set; }
    }
}