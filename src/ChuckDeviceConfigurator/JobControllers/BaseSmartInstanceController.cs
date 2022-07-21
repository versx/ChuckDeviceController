namespace ChuckDeviceConfigurator.JobControllers
{
    using System.Threading.Tasks;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    public abstract class BaseSmartInstanceController : IJobController
    {
        #region Variables

        private static readonly Random _random = new();
        private readonly ILogger<BaseSmartInstanceController> _logger;
        internal readonly Dictionary<string, DeviceIndex> _currentUuid = new();
        internal int _lastIndex = 0; // Used for basic leap frog routing only
        internal double _lastCompletedTime;
        internal double _lastLastCompletedTime;

        #endregion

        #region Properties

        public string Name { get; }

        public abstract IReadOnlyList<Coordinate> Coordinates { get; internal set; }

        public CircleInstanceType CircleType { get; }

        public CircleInstanceRouteType RouteType { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string GroupName { get; }

        public bool IsEvent { get; }

        public bool EnableLureEncounters { get; } // TODO: Make ILureJobController interface, remove from Base controller

        #endregion

        #region Constructor

        public BaseSmartInstanceController(Instance instance, List<Coordinate> coords, CircleInstanceType circleType, CircleInstanceRouteType routeType)
        {
            Name = instance.Name;
            Coordinates = coords;
            MinimumLevel = instance.MinimumLevel;
            MaximumLevel = instance.MaximumLevel;
            CircleType = circleType;
            RouteType = instance.Data?.CircleRouteType ?? routeType; // Strings.DefaultCircleRouteType;
            GroupName = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup;
            IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent;
            EnableLureEncounters = instance.Data?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters;

            _logger = new Logger<BaseSmartInstanceController>(LoggerFactory.Create(x => x.AddConsole()));
        }

        #endregion

        #region Public Methods

        public abstract Task<ITask> GetTaskAsync(TaskOptions options);

        public virtual async Task<string> GetStatusAsync()
        {
            var status = Strings.DefaultInstanceStatus;
            if (RouteType == CircleInstanceRouteType.Default)
            {
                // Gets the basic leap frog routing logic round time of any (the last) completed
                // device assigned to the route that has completed it.
                if (_lastCompletedTime > 0 && _lastLastCompletedTime > 0)
                {
                    var timeDiffSeconds = _lastCompletedTime - _lastLastCompletedTime;
                    if (timeDiffSeconds > 0)
                    {
                        var time = Math.Round(timeDiffSeconds, 2);
                        status = $"Round Time: {time:N0}s";
                    }
                }
            }
            else
            {
                // TODO: Get total average of all devices last completed time
                // Get the sum of all route round times for all devices if they have completed the
                // route at least once. Twice so that LastCompleted and LastCompletedWholeRoute are
                // both set. (maybe average too?)
                var totalRoundTime = _currentUuid.Values.Sum(device =>
                    // If device hasn't completed the route yet, or rather its assigned coordinates
                    // list, use 0 for route last completed sum calculation.
                    device.LastCompleted == 0 || device.LastCompletedWholeRoute == 0
                    ? 0
                    : (double)device.LastCompleted - (double)device.LastCompletedWholeRoute
                );
                if (totalRoundTime > 0)
                {
                    // Only show average route round time if at least one device has completed it.
                    var time = Math.Round(totalRoundTime);
                    // TODO: Format round trip time status by hours/minutes/seconds instead of just seconds.
                    status = $"Round Time: {time:N0}s";
                }
                else
                {
                    // No assigned devices have completed the route yet, show their current route indexes out
                    // of the total amount of coordinates for the route until one has fully completed it.
                    var indexes = _currentUuid.Values.Select(uuid => uuid.LastRouteIndex).ToList();
                    var indexesStatus = indexes.Count == 0
                        ? "0"
                        : string.Join(", ", _currentUuid.Values.Select(uuid => uuid.LastRouteIndex));
                    status = $"Route Indexes: {indexesStatus}/{Coordinates.Count}";
                }
            }
            return await Task.FromResult(status);
        }

        public abstract Task Reload();

        public abstract Task Stop();

        #endregion

        #region Routing Logic

        internal Coordinate BasicRoute()
        {
            var currentIndex = _lastIndex;
            var currentCoord = Coordinates[currentIndex];
            // Check if current index is last in coordinates list,
            // if so we've completed the route. Reset route to first
            // coordinate for next device.
            if (_lastIndex == Coordinates.Count)
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

        internal Coordinate SplitRoute(string uuid)
        {
            // TODO: Lock index
            var currentUuidIndex = _currentUuid.ContainsKey(uuid)
                ? _currentUuid[uuid].LastRouteIndex
                : Convert.ToInt32(Math.Round(Convert.ToDouble(_random.Next(ushort.MinValue, ushort.MaxValue) % Coordinates.Count)));

            var shouldAdvance = true;
            // TODO: Random from 0 to Coordinates.Count
            var offsetValue = _random.Next(ushort.MinValue, ushort.MaxValue) % 100;
            if (offsetValue < 5)
            {
                // Use a light hand and 25% of the time try to space out devices
                // this ensures average round time decreases by at least 10% using
                // this approach
                (uint numLiveDevices, double distanceToNextDevice) = GetDeviceSpacing(uuid);
                if (numLiveDevices == 0 && distanceToNextDevice == 0)
                {
                    // Failed to calculate device spacing, probably no devices
                    _logger.LogError($"[{Name}] [{uuid}] Failed to calculate spacing between devices in order to get next location, skipping device...");
                    return null;
                }

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
            if (shouldAdvance)
            {
                currentUuidIndex++;
                if (currentUuidIndex >= Coordinates.Count)
                {
                    currentUuidIndex = 0;
                    // This is an approximation of round time per device
                    _currentUuid[uuid].LastCompletedWholeRoute = _currentUuid[uuid].LastCompleted;
                    _currentUuid[uuid].LastCompleted = now;
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

            _currentUuid[uuid].LastRouteIndex = currentUuidIndex;
            _currentUuid[uuid].LastSeen = now;

            var currentCoord = Coordinates[currentUuidIndex];
            return currentCoord;
        }

        internal Coordinate SmartRoute(string uuid)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            // TODO: ContainsKey check is probably redundant since we add the device to the current
            // device list in the GetTaskAsync method. Need to confirm but 99.99% possitive the random
            // index is never used.
            // Which I guess if we remove AddDevice from GetTask, random index would be generated.

            // Check if current device cache contains device and that the last route index is greater
            // than 0 as well as more than one device is assigned to the instance. Otherwise give the
            // device a random route index to start from.
            var currentUuidIndex = _currentUuid.ContainsKey(uuid) && _currentUuid.Count > 0
                ? _currentUuid[uuid].LastRouteIndex
                : _random.Next(0, Coordinates.Count);

            _currentUuid[uuid].LastRouteIndex = currentUuidIndex;
            _currentUuid[uuid].LastSeen = now;

            var shouldAdvance = true;
            var jumpDistance = 0d;

            if (_currentUuid.Count > 1 && _random.Next(0, 100) < 15)
            {
                (uint numLiveDevices, double distanceToNextDevice) = GetDeviceSpacing(uuid);
                if (numLiveDevices == 0 && distanceToNextDevice == 0)
                {
                    // Failed to calculate device spacing, probably no devices
                    _logger.LogError($"[{Name}] [{uuid}] Failed to calculate spacing between devices in order to get next location, skipping device...");
                    return null;
                }

                // TODO: This looks like some wizardry which could possibly be optimized,
                // and potentially calculated better. :thinking:
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

            // Advance device if route index is 0 (beginning) and that there are more than one coordinate specified
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
                    _currentUuid[uuid].LastCompletedWholeRoute = _currentUuid[uuid].LastCompleted;
                    _currentUuid[uuid].LastCompleted = now;
                }
            }
            else
            {
                // Device should not advance forward int he route, check if current device index
                // is less than 0 after backing up incase we are at the first coordinate. If device's
                // current index is less than 0 set route index to last coord in list.
                currentUuidIndex--;
                if (currentUuidIndex < 0)
                {
                    currentUuidIndex = Coordinates.Count;
                }
            }

            // Last current device's last route index taken
            _currentUuid[uuid].LastRouteIndex = currentUuidIndex;
            var currentCoord = Coordinates[currentUuidIndex];
            return currentCoord;
        }

        #endregion

        #region Private Methods

        internal virtual CircleTask CreateTask(Coordinate coord, CircleInstanceType circleType = CircleInstanceType.Pokemon)
        {
            return new CircleTask
            {
                Area = Name,
                Action = circleType == CircleInstanceType.Pokemon
                    ? DeviceActionType.ScanPokemon
                    : DeviceActionType.ScanRaid,
                Latitude = coord.Latitude,
                Longitude = coord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
                LureEncounter = EnableLureEncounters,
            };
        }

        private double GetRouteDistanceToNextDevice(double x, double y)
        {
            return x < y
                ? y - x
                : y + (Coordinates.Count - x);
        }

        private (uint, double) GetDeviceSpacing(string uuid)
        {
            var liveDevices = GetLiveDevices();
            var numLiveDevices = liveDevices.Count;
            var distanceToNextDevice = (double)Coordinates.Count;

            // Check if device is in live device list, possibly a redunant check, need to confirm.
            if (!liveDevices.Contains(uuid))
            {
                _logger.LogWarning($"[{Name}] [{uuid}] Device is not in live device list, unable to calculate device spacing for job controller instance.");
                return (0, 0);
            }

            for (var i = 0; i < numLiveDevices; i++)
            {
                // Skip device if not in live device list
                if (uuid != liveDevices[i])
                    continue;

                var nextDeviceIndex = i < numLiveDevices - 1
                    ? i + 1
                    : i; // TODO: Check, was '0'
                var nextDeviceUuid = liveDevices[nextDeviceIndex];
                var currentUuidIndex = _currentUuid[uuid].LastRouteIndex;
                var nextUuidIndex = _currentUuid[nextDeviceUuid].LastRouteIndex;
                distanceToNextDevice = GetRouteDistanceToNextDevice(currentUuidIndex, nextUuidIndex);
            }

            return ((uint)numLiveDevices, distanceToNextDevice);
        }

        internal void AddDevice(string uuid)
        {
            // Check if device already exists
            if (_currentUuid.ContainsKey(uuid))
                return;

            _currentUuid.Add(uuid, new DeviceIndex
            {
                // TODO: Set/generate random device index upon adding to device list?
                LastSeen = DateTime.UtcNow.ToTotalSeconds(),
            });
        }

        private List<string> GetLiveDevices()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var deadDeviceCutOffTime = now - 60;
            var liveDevices = new List<string>();

            // Sort indexes by current device index
            var deviceIndexes = GetDevicesSortedByIndex();
            foreach (var (currentUuid, index) in deviceIndexes)
            {
                // Check if device has requested a job, otherwise skip it
                if (index.LastSeen == 0)
                    continue;

                if (index.LastSeen < deadDeviceCutOffTime)
                {
                    // Device has not updated in the last 60 seconds, assume dead/offline
                    _currentUuid.Remove(currentUuid);
                }
                else
                {
                    // Device is active and alive, add to live devices list
                    liveDevices.Add(currentUuid);
                }
            }
            return liveDevices;
        }

        private Dictionary<string, DeviceIndex> GetDevicesSortedByIndex()
        {
            var devices = _currentUuid.ToList();
            devices.Sort((pair1, pair2) => pair1.Value.LastRouteIndex.CompareTo(pair2.Value.LastRouteIndex));

            var indexes = devices.ToDictionary(key => key.Key, value => value.Value);
            return indexes;
        }

        #endregion
    }
}