namespace ChuckDeviceController.JobControllers.Instances
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Chuck.Infrastructure.Common;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.JobControllers;
    using Chuck.Infrastructure.JobControllers.Tasks;

    public class CircleInstanceController : IJobController
    {
        #region Variables

        private readonly ILogger<CircleInstanceController> _logger;

        private readonly Dictionary<string, DeviceIndex> _lastUuid;
        private static readonly Random _random = new Random();
        private DateTime _lastCompletedTime;
        private int _lastIndex;
        private DateTime _lastLastCompletedTime;

        private readonly object _indexLock = new object();

        #endregion

        #region Properties

        public string Name { get; set; }

        public List<Coordinate> Coordinates { get; }

        public CircleType Type { get; }

        public CircleRouteType RouteType { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        #endregion

        #region Constructor

        public CircleInstanceController(string name, List<Coordinate> coords, CircleType type, CircleRouteType routeType, ushort minLevel, ushort maxLevel)
        {
            Name = name;
            Coordinates = coords;
            Type = type;
            RouteType = routeType;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;

            _logger = new Logger<CircleInstanceController>(LoggerFactory.Create(x => x.AddConsole()));

            _lastCompletedTime = DateTime.UtcNow;
            _lastUuid = new Dictionary<string, DeviceIndex>();
            _lastIndex = 0;
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTask(string uuid, string accountUsername, bool startup)
        {
            AddDevice(uuid);
            Coordinate currentCoord = null;
            switch (Type)
            {
                case CircleType.Pokemon:
                case CircleType.Raid:
                    switch (RouteType)
                    {
                        case CircleRouteType.Default:
                            // Get default leap frog route
                            currentCoord = BasicRoute(uuid, startup);
                            break;
                        case CircleRouteType.Split:
                            // Get split route by device count
                            currentCoord = SplitRoute(uuid, startup);
                            break;
                        case CircleRouteType.Circular:
                            // Get circular split route by device count
                            currentCoord = CircleRoute(uuid);
                            break;
                    }
                    break;
            }
            if (currentCoord == null)
                return null;

            return await Task.FromResult(new CircleTask
            {
                Action = Type == CircleType.Pokemon
                    ? ActionType.ScanPokemon
                    : ActionType.ScanRaid,
                Area = Name,
                Latitude = currentCoord.Latitude,
                Longitude = currentCoord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            }).ConfigureAwait(false);
        }

        public async Task<string> GetStatus()
        {
            var text = "--";
            if (_lastCompletedTime != default && _lastLastCompletedTime != default)
            {
                var timeDiffSeconds = _lastCompletedTime.Subtract(_lastLastCompletedTime).TotalSeconds;
                if (timeDiffSeconds > 0)
                {
                    text = $"Round Time {Math.Round(timeDiffSeconds, 2):N0}s";
                }
            }
            return await Task.FromResult(text).ConfigureAwait(false);
        }

        public void Stop()
        {
        }

        public void Reload()
        {
            _lastIndex = 0;
            _lastCompletedTime = default;
            _lastLastCompletedTime = default;
        }

        #endregion

        #region Private Methods

        private double GetRouteDistance(double x, double y)
        {
            if (x < y)
            {
                return y - x;
            }
            return y + (Coordinates.Count - x);
        }

        private (int, double) QueryLiveDevices(string uuid, int index)
        {
            // In seconds
            var deadDeviceCutoff = DateTime.UtcNow.ToTotalSeconds() - 60;
            var numLiveDevices = 1;
            var distanceToNext = (double)Coordinates.Count;
            foreach (var (oldUuid, value) in _lastUuid)
            {
                if (string.Compare(oldUuid, uuid, true) != 0)
                    continue;

                if (value?.LastSeen == null)
                    continue;

                var sec = value.LastSeen.ToTotalSeconds();
                if (sec > deadDeviceCutoff)
                {
                    numLiveDevices++;
                    var distance = GetRouteDistance(index, value.Index);
                    if (distance < distanceToNext)
                    {
                        distanceToNext = distance;
                    }
                }
            }
            return (numLiveDevices, distanceToNext);
        }

        private void AddDevice(string uuid)
        {
            // If device is not in lastUuid dictionary, add it with default index of 0
            if (!_lastUuid.ContainsKey(uuid))
            {
                _lastUuid.Add(uuid, new DeviceIndex
                {
                    // TODO: Maybe instead of 0, use a random number between 0 and Coordinates.Count
                    Index = 0,
                    LastSeen = DateTime.UtcNow,
                });
            }
        }

        #endregion

        #region Route Methods

        private Coordinate BasicRoute(string uuid, bool startup = false)
        {
            lock (_indexLock)
            {
                var currentIndex = _lastIndex;
                _logger.LogDebug($"[{uuid}] Current index: {currentIndex}");
                var currentCoord = Coordinates[currentIndex];
                if (!startup)
                {
                    if (_lastIndex + 1 == Coordinates.Count)
                    {
                        _lastLastCompletedTime = _lastCompletedTime;
                        _lastCompletedTime = DateTime.UtcNow;
                        _lastIndex = 0;
                    }
                    else
                    {
                        _lastIndex++;
                    }
                }
                return currentCoord;
            }
        }

        private Coordinate SplitRoute(string uuid, bool startup = false)
        {
            lock (_indexLock)
            {
                var currentUuidIndex = _lastUuid.ContainsKey(uuid)
                    ? _lastUuid[uuid].Index
                    : Convert.ToInt32(Math.Round(Convert.ToDouble(_random.Next(ushort.MinValue, ushort.MaxValue) % Coordinates.Count)));
                _logger.LogDebug($"[{uuid}] Current index: {currentUuidIndex}");
                var shouldAdvance = true;
                var offsetValue = _random.Next(ushort.MinValue, ushort.MaxValue) % 100;
                if (offsetValue < 5)
                {
                    // Use a light hand and 25% of the time try to space out devices
                    // this ensures average round time decreases by at least 10% using
                    // this approach
                    var (numDevices, distanceToNextDevice) = QueryLiveDevices(uuid, currentUuidIndex);
                    var dist = Convert.ToInt32((numDevices * distanceToNextDevice) + 0.5);
                    if (dist < Coordinates.Count)
                    {
                        shouldAdvance = false;
                    }
                }
                if (currentUuidIndex == 0)
                {
                    // Don't back up past 0 to avoid round time inaccuracy
                    shouldAdvance = true;
                }
                if (!startup)
                {
                    if (shouldAdvance)
                    {
                        currentUuidIndex++;
                        if (currentUuidIndex >= Coordinates.Count - 1)
                        {
                            currentUuidIndex = 0;
                            // This is an approximation of round time.
                            _lastLastCompletedTime = _lastCompletedTime;
                            _lastCompletedTime = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Back up!
                        currentUuidIndex--;
                        if (currentUuidIndex < 0)
                        {
                            currentUuidIndex = Coordinates.Count - 1;
                        }
                    }
                }
                _lastUuid[uuid].Index = currentUuidIndex;
                _lastUuid[uuid].LastSeen = DateTime.UtcNow;
                return Coordinates[currentUuidIndex];
            }
        }

        // Credits: https://github.com/RealDeviceMap/RealDeviceMap/pull/184
        private Coordinate CircleRoute(string uuid)
        {
            lock (_indexLock)
            {
                var currentUuidIndex = _lastUuid.ContainsKey(uuid)
                    ? _lastUuid[uuid].Index
                    : Convert.ToInt32(Math.Round(Convert.ToDouble(_random.Next(ushort.MinValue, ushort.MaxValue) % Coordinates.Count)));
                _lastUuid[uuid].LastSeen = DateTime.UtcNow;
                var shouldAdvance = true;
                var jumpDistance = 0d;
                if (_lastUuid.Count > 1 && _random.Next(0, 100) < 15)
                {
                    var (numLiveDevices, distanceToNext) = QueryLiveDevices(uuid, currentUuidIndex);
                    var dist = (10 * distanceToNext * numLiveDevices) + 5;
                    if (dist < 10 * Coordinates.Count)
                    {
                        shouldAdvance = false;
                    }
                    if (dist > 12 * Coordinates.Count)
                    {
                        jumpDistance = distanceToNext - (Coordinates.Count / numLiveDevices) - 1;
                    }
                }
                if (currentUuidIndex == 0)
                {
                    shouldAdvance = true;
                }
                if (shouldAdvance)
                {
                    currentUuidIndex += (int)jumpDistance + 1;
                    if (currentUuidIndex >= Coordinates.Count - 1)
                    {
                        currentUuidIndex -= Coordinates.Count - 1;
                        _lastLastCompletedTime = _lastCompletedTime;
                        _lastCompletedTime = DateTime.UtcNow;
                    }
                }
                else
                {
                    currentUuidIndex--;
                    if (currentUuidIndex < 0)
                    {
                        currentUuidIndex = Coordinates.Count - 1;
                    }
                }
                _logger.LogDebug($"[{uuid}] Current index: {currentUuidIndex}");
                _lastUuid[uuid].Index = currentUuidIndex;
                return Coordinates[currentUuidIndex];
            }
        }

        #endregion
    }
}