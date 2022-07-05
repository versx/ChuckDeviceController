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
        private readonly Dictionary<string, DeviceIndex> _currentUuid = new();
        private Queue<Coordinate> _scanNextCoords = new(); 
        private uint _lastIndex = 0; // Used for basic leap frog routing
        private double _lastCompletedTime;
        private double _lastLastCompletedTime;

        #region Properties

        public string Name { get; }

        public IReadOnlyList<Coordinate> Coordinates { get; }

        public CircleInstanceType CircleType { get; }

        public CircleInstanceRouteType RouteType { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string? GroupName { get; } // TODO: Fix warning

        public bool IsEvent { get; }

        public bool SendTaskForLureEncounter { get; set; } // TODO: Make configurable

        #endregion

        #region Constructor

        public CircleInstanceController(Instance instance, List<Coordinate> coords, CircleInstanceType circleType)
        {
            Name = instance.Name;
            Coordinates = coords;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            CircleType = circleType;
            RouteType = instance.Data?.CircleRouteType ?? CircleInstanceRouteType.Default;
            GroupName = instance.Data?.AccountGroup ?? null;
            IsEvent = instance.Data?.IsEvent ?? false;
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(string uuid, string? accountUsername = null, Account? account = null, bool isStartup = false)
        {
            // Add device to device list
            AddDevice(uuid);
            Coordinate? currentCoord = null;

            if (Coordinates.Count == 0)
            {
                // TODO: Throw error that instance requires at least one coordinate
                return null;
            }

            // Check if on demand scanning coordinates list has any to send to workers
            if (_scanNextCoords.Count > 0)
            {
                currentCoord = _scanNextCoords.Dequeue();
                return new CircleTask
                {
                    Action = DeviceActionType.ScanPokemon,
                    Area = Name,
                    Latitude = currentCoord.Latitude,
                    Longitude = currentCoord.Longitude,
                    MinimumLevel = MinimumLevel,
                    MaximumLevel = MaximumLevel,
                };
            }

            switch (CircleType)
            {
                case CircleInstanceType.Pokemon:
                case CircleInstanceType.Raid:
                    switch (RouteType)
                    {
                        case CircleInstanceRouteType.Default:
                            // Get default leap frog route
                            currentCoord = BasicRoute();
                            break;
                        case CircleInstanceRouteType.Split:
                            // Split route by device count
                            currentCoord = SplitRoute(uuid);
                            break;
                        //case CircleInstanceRouteType.Circular:
                            // Circular split route by device count
                        case CircleInstanceRouteType.Smart:
                            // Smart routing by device count
                            currentCoord = SmartRoute(uuid);
                            break;
                    }
                    break;
            }

            // Check if we were unable to retrieve a coordinate to send
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
            if (//_lastCompletedTime != default && _lastLastCompletedTime != default ||
                _lastCompletedTime > 0 && _lastLastCompletedTime > 0)
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

        #region Routing Logic

        private Coordinate BasicRoute()
        {
            var currentIndex = (int)_lastIndex;
            var currentCoord = Coordinates[currentIndex];
            // Check if current index is last in coordinates list,
            // if so we've completed the route. Reset route to first
            // coordinate for next device.
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
            return currentCoord;
        }

        private Coordinate SplitRoute(string uuid, bool isStartup = false)
        {
            // TODO: Lock index
            var currentUuidIndex = _currentUuid.ContainsKey(uuid)
                ? _currentUuid[uuid].Index
                : Convert.ToInt32(Math.Round(Convert.ToDouble(_random.Next(ushort.MinValue, ushort.MaxValue) % Coordinates.Count)));

            var shouldAdvance = true;
            var offsetValue = _random.Next(ushort.MinValue, ushort.MaxValue) % 100;
            if (offsetValue < 5)
            {
                // Use a light hand and 25% of the time try to space out devices
                // this ensures average round time decreases by at least 10% using
                // this approach
                var (numLiveDevices, distanceToNextDevice) = CheckDeviceSpacing(uuid);
                var dist = Convert.ToInt32((numLiveDevices * distanceToNextDevice) + 0.5);
                if (dist < Coordinates.Count)
                {
                    shouldAdvance = false;
                }
            }
            if (currentUuidIndex == 0)
            {
                // Don't back up past 0 to avoid route time inaccuracy
                shouldAdvance = true;
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            if (!isStartup)
            {
                if (shouldAdvance)
                {
                    currentUuidIndex++;
                    if (currentUuidIndex >= Coordinates.Count)
                    {
                        currentUuidIndex = 0;
                        // This is an approximation of round time.
                        _lastLastCompletedTime = _lastCompletedTime;
                        _lastCompletedTime = now;
                    }
                }
                else
                {
                    // Back up!
                    currentUuidIndex--;
                    if (currentUuidIndex < 0)
                    {
                        currentUuidIndex = Coordinates.Count;
                    }
                }
            }
            _currentUuid[uuid] = new DeviceIndex
            {
                Index = currentUuidIndex,
                LastSeen = now,
            };
            var currentCoord = Coordinates[currentUuidIndex];
            return currentCoord;
        }

        private Coordinate SmartRoute(string uuid)
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
                var (numLiveDevices, distanceToNextDevice) = CheckDeviceSpacing(uuid);
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

        #endregion

        #region Helpers

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

            /*
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
            */

            var numLiveDevices = liveDevices.Count;
            double distanceToNextDevice = Coordinates.Count;
            for (var i = 0; i < numLiveDevices; i++)
            {
                if (uuid != liveDevices[i])
                {
                    continue;
                }
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
            if (!_currentUuid.ContainsKey(uuid))
            {
                //var now = DateTime.UtcNow.ToTotalSeconds();
                _currentUuid.Add(uuid, new DeviceIndex
                {
                    //Index = 0,
                    //LastSeen = now,
                });
            }
        }

        #endregion
    }

    public class DeviceIndex
    {
        public int Index { get; set; }

        public ulong LastSeen { get; set; }
    }
}