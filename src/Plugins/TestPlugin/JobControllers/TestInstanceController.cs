namespace TestPlugin.JobControllers
{
    using System;
    using System.Text.Json.Serialization;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.Common.Tasks;

    /* Available interfaces to extend job controller:
     * IJobControllerCoordinates - Adds coordinates list vs geofence
     * IScanNextInstanceController - Enables on-demand Pokemon encountering
     * ILureInstanceController - Enables nearby lure (MapPokemon) encountering
     * IEventInstanceController - Enables event specific Pokemon re-encountering (IJobController already inherits from this)
     */

    public class TestInstanceController : IJobController, IJobControllerCoordinates, IScanNextInstanceController
    {
        #region Variables

        internal int _lastIndex = 0;
        internal double _lastCompletedTime;
        internal double _lastLastCompletedTime;

        #endregion

        #region Properties

        public string Name { get; }

        public IReadOnlyList<ICoordinate> Coordinates { get; }

        public ushort MinimumLevel { get; }

        public ushort MaximumLevel { get; }

        public string? GroupName { get; }

        public bool IsEvent { get; }

        public Queue<ICoordinate> ScanNextCoordinates { get; } = new();

        #endregion

        #region Constructor

        public TestInstanceController(string name, ushort minLevel, ushort maxLevel,
            List<Coordinate> coords, string? groupName = null, bool isEvent = false)
        {
            Name = name;
            MinimumLevel = minLevel;
            MaximumLevel = maxLevel;
            Coordinates = coords;
            GroupName = groupName;
            IsEvent = isEvent;
        }

        #endregion

        #region Public Methods

        public async Task<ITask> GetTaskAsync(TaskOptions options)
        {
            // Check if on demand scanning coordinates list has any to send to workers
            if (ScanNextCoordinates.Count > 0)
            {
                var coord = ScanNextCoordinates.Dequeue();
                var scanNextTask = CreateTask(coord);
                Console.WriteLine($"[{Name}] [{options.Uuid}] Executing ScanNext API job at '{coord}'");
                return await Task.FromResult(scanNextTask);
            }

            var currentIndex = _lastIndex;
            if (!(Coordinates?.Any() ?? false))
            {
                Console.WriteLine($"[{Name}] [{options.Uuid}] Instance requires at least one coordinate, returning empty task for device");
                return new TestTask();
            }

            // Get next scan coordinate for device based on route type
            var currentCoord = Coordinates![currentIndex];
            // Check if current index is last in coordinates list,
            // if so we've completed the route. Reset route to first
            // coordinate for next device.
            if (_lastIndex == Coordinates.Count - 1)
            {
                _lastLastCompletedTime = _lastCompletedTime;
                _lastCompletedTime = GetUnixTimestamp();
                _lastIndex = 0;
            }
            else
            {
                _lastIndex++;
            }

            // Check if we were unable to retrieve a coordinate to send
            if (currentCoord == null)
            {
                Console.WriteLine($"[{Name}] [{options.Uuid}] Failed to retrieve next scan coordinate");
                return new TestTask();
            }

            var task = CreateTask(currentCoord);
            return await Task.FromResult(task);
        }

        public async Task<string> GetStatusAsync()
        {
            var status = "--";
            // device assigned to the route that has completed it.
            if (_lastCompletedTime == 0 || _lastLastCompletedTime == 0)
                return status;

            var timeDiffSeconds = _lastCompletedTime - _lastLastCompletedTime;
            if (timeDiffSeconds > 0)
            {
                var time = Math.Round(timeDiffSeconds, 2);
                status = $"Round Time: {time:N0}s";
            }
            return await Task.FromResult(status);
        }

        public Task ReloadAsync()
        {
            Console.WriteLine($"[{Name}] Reloading instance");

            _lastIndex = 0;

            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            Console.WriteLine($"[{Name}] Stopping instance");

            return Task.CompletedTask;
        }

        #endregion

        private TestTask CreateTask(ICoordinate coord)
        {
            return new TestTask
            {
                Action = DeviceActionType.ScanPokemon,
                Latitude = coord.Latitude,
                Longitude = coord.Longitude,
                MinimumLevel = MinimumLevel,
                MaximumLevel = MaximumLevel,
            };
        }

        private static ulong GetUnixTimestamp()
        {
            var epoch = new DateTime(1970, 1, 1);
            var time = DateTime.UtcNow.Subtract(epoch);
            var timestamp = Convert.ToUInt64(time.TotalSeconds);
            return timestamp;
        }
    }

    public class TestTask : ITask
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("min_level")]
        public ushort MinimumLevel { get; set; }

        [JsonPropertyName("max_level")]
        public ushort MaximumLevel { get; set; }
    }
}