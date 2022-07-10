namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceConfigurator.Extensions;
    using ChuckDeviceConfigurator.JobControllers;
    using ChuckDeviceConfigurator.Services.Geofences;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.TimeZone;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Geometry.Models;

    using Microsoft.EntityFrameworkCore;

    // TODO: HostedService?
    public class JobControllerService : IJobControllerService
    {
        #region Variables

        private readonly ILogger<IJobControllerService> _logger;
        private readonly IDbContextFactory<DeviceControllerContext> _deviceFactory;
        private readonly IDbContextFactory<MapDataContext> _mapFactory;
        //private readonly IConfiguration _configuration;
        private readonly ITimeZoneService _timeZoneService;
        private readonly IGeofenceControllerService _geofenceService;
        //private readonly IAssignmentControllerService _assignmentService;
        private readonly IRouteGenerator _routeGenerator;

        private readonly IDictionary<string, Device> _devices;
        private readonly IDictionary<string, IJobController> _instances;

        private readonly object _devicesLock = new();
        private readonly object _instancesLock = new();

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, Device> Devices =>
            (IReadOnlyDictionary<string, Device>)_devices;

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyDictionary<string, IJobController> Instances =>
            (IReadOnlyDictionary<string, IJobController>)_instances;

        #endregion

        #region Constructor

        public JobControllerService(
            ILogger<IJobControllerService> logger,
            IDbContextFactory<DeviceControllerContext> deviceFactory,
            IDbContextFactory<MapDataContext> mapFactory,
            //IConfiguration configuration,
            ITimeZoneService timeZoneService,
            IGeofenceControllerService geofenceService,
            IRouteGenerator routeGenerator)
            //IAssignmentControllerService assignmentService)
        {
            _logger = logger;
            _deviceFactory = deviceFactory;
            _mapFactory = mapFactory;
            //_configuration = configuration;
            _timeZoneService = timeZoneService;
            _geofenceService = geofenceService;
            _routeGenerator = routeGenerator;
            //_assignmentService = assignmentService;

            _devices = new Dictionary<string, Device>();
            _instances = new Dictionary<string, IJobController>();
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            using (var context = _deviceFactory.CreateDbContext())
            {
                var instances = context.Instances.ToList();
                var devices = context.Devices.ToList();

                foreach (var instance in instances)
                {
                    if (!ThreadPool.QueueUserWorkItem(async _ =>
                    {
                        _logger.LogInformation($"Starting instance {instance.Name}");
                        await AddInstanceAsync(instance);
                        _logger.LogInformation($"Started instance {instance.Name}");

                        var newDevices = devices.AsEnumerable()
                                                .Where(d => string.Compare(d.InstanceName, instance.Name, true) == 0);
                        foreach (var device in newDevices)
                        {
                            AddDevice(device);
                        }
                    }))
                    {
                        _logger.LogError($"Failed to start instance {instance.Name}");
                    }
                }
            }
            _logger.LogInformation("All instances have been started");
        }

        public void Stop()
        {
            foreach (var (name, jobController) in _instances)
            {
                _logger.LogInformation($"[{name}] Stopping job controller");
                jobController.Stop();
            }
        }

        #endregion

        #region Instances

        public async Task AddInstanceAsync(Instance instance)
        {
            _logger.LogDebug($"Adding instance {instance.Name}");

            if (string.IsNullOrEmpty(instance.Name))
            {
                _logger.LogError($"No instance name set, skipping instance creation...");
                return;
            }

            var geofences = _geofenceService.GetGeofences(instance.Geofences);
            if (geofences == null)
            {
                _logger.LogError($"[{instance.Name}] Failed to get geofences for instance, make sure it is assigned at least one");
                return;
            }

            /*
            try
            {
                foreach (var geofence in geofences)
                {
                    if (geofence.Name == "CircleTestCalc")
                    {
                        var coords = geofence.ConvertToCoordinates();
                        var calc = new RouteCalculator(coords);
                        var routeCoords = calc.CalculateShortestRoute(coords.FirstOrDefault());
                        _logger.LogInformation($"RouteCoords: {routeCoords}");
                    }
                    else if (geofence.Name == "Montclair Geofence")
                    {
                        var stopwatch = new System.Diagnostics.Stopwatch();
                        stopwatch.Start();
                        var area = geofence.ConvertToMultiPolygons();
                        var multiPolygons = area.Item1;
                        var route = _routeGenerator.GenerateBootstrapRoute(multiPolygons);
                        stopwatch.Stop();
                        var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 4);
                        _logger.LogInformation($"Took {totalSeconds}s");

                        var randomRoute = _routeGenerator.GenerateRandomRoute(multiPolygons);
                        _logger.LogInformation($"Random: {randomRoute}");

                        var calc = new RouteCalculator(randomRoute);
                        var optimizedRoute = calc.CalculateShortestRoute(randomRoute.FirstOrDefault());
                        _logger.LogInformation($"Optimized: {optimizedRoute}");

                        //var route = _routeGenerator.GenerateOptimizedRoute(multiPolygon, 300);
                        _logger.LogDebug($"Route: {route}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex}");
            }
            */

            IJobController? jobController = null;
            switch (instance.Type)
            {
                case InstanceType.CirclePokemon:
                case InstanceType.CircleSmartPokemon:
                case InstanceType.CircleRaid:
                case InstanceType.CircleSmartRaid:
                    var coords = geofences.ConvertToCoordinates();
                    switch (instance.Type)
                    {
                        case InstanceType.CirclePokemon:
                        case InstanceType.CircleSmartPokemon:
                            jobController = CreateCircleJobController(instance, CircleInstanceType.Pokemon, coords);
                            break;
                        case InstanceType.CircleRaid:
                            jobController = CreateCircleJobController(instance, CircleInstanceType.Raid, coords);
                            break;
                        case InstanceType.CircleSmartRaid:
                            jobController = CreateCircleSmartRaidJobController(_mapFactory, instance, coords);
                            break;
                    }
                    break;
                case InstanceType.AutoQuest:
                case InstanceType.PokemonIV:
                case InstanceType.Bootstrap:
                case InstanceType.FindTth:
                    var (multiPolygons, coordinates) = geofences.ConvertToMultiPolygons();
                    switch (instance.Type)
                    {
                        case InstanceType.AutoQuest:
                            var timeZone = instance.Data?.TimeZone;
                            var timeZoneOffset = ConvertTimeZoneToOffset(timeZone, instance.Data?.EnableDst ?? Strings.DefaultEnableDst);
                            jobController = CreateAutoQuestJobController(_mapFactory, _deviceFactory, instance, multiPolygons, timeZoneOffset);
                            // TODO: Implement AutoInstanceController.InstanceComplete event
                            // ((AutoInstanceController)jobController).InstanceComplete += (sender, e) => _assignmentService.InstanceControllerDone(e.InstanceName);
                            break;
                        case InstanceType.Bootstrap:
                            jobController = new BootstrapInstanceController(instance, coordinates);
                            break;
                        case InstanceType.FindTth:
                            jobController = new TthFinderInstanceController(instance, coordinates);
                            break;
                        case InstanceType.PokemonIV:
                            var ivList = await GetIvListAsync(instance.Data?.IvList ?? null);
                            if (ivList == null)
                            {
                                _logger.LogError($"Failed to fetch IV list for instance {instance.Name}, skipping controller instantiation...");
                                return;
                            }
                            jobController = CreateIvJobController(instance, multiPolygons, ivList);
                            break;
                    }
                    break;
            }

            if (jobController == null)
            {
                _logger.LogError($"[{instance.Name}] Unable to instantiate job instance controller with instance name '{instance.Name}' and type '{instance.Type}'");
                return;
            }

            lock (_instancesLock)
            {
                _instances[instance.Name] = jobController;
            }

            await Task.CompletedTask;
        }

        public IJobController GetInstanceController(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                _logger.LogError($"Failed to get job controller instance for device, UUID was null");
                return null;
            }

            lock (_devicesLock)
            {
                if (!_devices.ContainsKey(uuid))
                {
                    _logger.LogWarning($"[{uuid}] Device is not assigned an instance!");
                    return null;
                }

                var device = _devices[uuid];
                var instanceName = device?.InstanceName;
                if (device == null || string.IsNullOrEmpty(instanceName))
                {
                    _logger.LogWarning($"Device or device instance name was null, unable to retrieve job controller instance");
                    return null;
                }

                return GetInstanceControllerByName(instanceName);
            }
        }

        public async Task<string> GetStatusAsync(Instance instance)
        {
            IJobController jobController;
            lock (_instancesLock)
            {
                if (!_instances.ContainsKey(instance.Name))
                {
                    // Instance not started or added to instance cache yet
                    return "Starting...";
                }

                jobController = _instances[instance.Name];
            }
            if (jobController != null)
            {
                return await jobController.GetStatusAsync();
            }

            return "Error";
        }

        public void ReloadAllInstances()
        {
            lock (_instancesLock)
            {
                foreach (var (_, instanceController) in _instances)
                {
                    instanceController?.Reload();
                }
            }
            // TODO: _assignmentService.Reload();
        }

        public async Task ReloadInstanceAsync(Instance newInstance, string oldInstanceName)
        {
            lock (_instancesLock)
            {
                if (!_instances.ContainsKey(oldInstanceName))
                {
                    _logger.LogError($"[{oldInstanceName}] Instance does not exist in instance cache, skipping instance reload...");
                    return;
                }

                var oldInstance = _instances[oldInstanceName];
                if (oldInstance != null)
                {
                    var devices = _devices.Where(device => string.Compare(device.Value.InstanceName, oldInstance.Name, true) == 0);
                    foreach (var (uuid, device) in devices)
                    {
                        device.InstanceName = newInstance.Name;
                        _devices[uuid] = device;
                    }
                    _instances[oldInstanceName]?.Stop();
                    _instances[oldInstanceName] = null;
                }
            }

            //await RemoveInstanceAsync(oldInstanceName);
            await AddInstanceAsync(newInstance);
        }

        public async Task RemoveInstanceAsync(string instanceName)
        {
            lock (_instancesLock)
            {
                _instances[instanceName]?.Stop();
                _instances[instanceName] = null;
                _instances.Remove(instanceName);
            }

            lock (_devicesLock)
            {
                var devices = _devices.Where(device => string.Compare(device.Value.InstanceName, instanceName, true) == 0);
                foreach (var device in devices)
                {
                    _devices[device.Key] = null;
                }
            }

            // TODO: _assignmentService.Reload();
            await Task.CompletedTask;
        }

        #endregion

        #region Devices

        public void AddDevice(Device device)
        {
            lock (_devicesLock)
            {
                if (!_devices.ContainsKey(device.Uuid))
                {
                    _devices.Add(device.Uuid, device);
                }
            }
            // TODO: _assignmentService.Reload();
        }

        public List<string> GetDeviceUuidsInInstance(string instanceName)
        {
            var uuids = new List<string>();
            lock (_devicesLock)
            {
                foreach (var (uuid, device) in _devices)
                {
                    if (string.Compare(device.InstanceName, instanceName, true) == 0)
                    {
                        uuids.Add(uuid);
                    }
                }
            }
            return uuids;
        }

        public void ReloadDevice(Device newDevice, string oldDeviceUuid)
        {
            RemoveDevice(oldDeviceUuid);
            AddDevice(newDevice);
        }

        public async Task RemoveDeviceAsync(Device device)
        {
            RemoveDevice(device.Uuid);

            // TODO: _assignmentService.Reload();
            await Task.CompletedTask;
        }

        public void RemoveDevice(string uuid)
        {
            lock (_devicesLock)
            {
                if (!_devices.ContainsKey(uuid))
                {
                    _logger.LogError($"[{uuid}] Unable to remove device from cache, it does not exist");
                    return;
                }
                _devices.Remove(uuid);
            }
            // TODO: _assignmentService.Reload();
        }

        #endregion

        #region Private Methods

        private IJobController GetInstanceControllerByName(string name)
        {
            lock (_instancesLock)
            {
                if (!_instances.ContainsKey(name))
                {
                    _logger.LogError($"[{name}] Unable to get instance controller by name, it does not exist in cache");
                    return null;
                }
                return _instances[name];
            }
        }

        private async Task<IvList> GetIvListAsync(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogError($"IV list name for IV instance is null, skipping job controller instantiation...");
                return null;
            }

            using (var context = _deviceFactory.CreateDbContext())
            {
                var ivList = await context.IvLists.FindAsync(name);
                return ivList;
            }
        }

        private short ConvertTimeZoneToOffset(string? timeZone = null, bool enableDst = false)
        {
            short timeZoneOffset = 0;
            // Check if time zone is set
            if (string.IsNullOrEmpty(timeZone))
            {
                // If no time zone set, return UTC offset
                return timeZoneOffset;
            }

            // Check if time zone service contains out time zone name
            if (!_timeZoneService.TimeZones.ContainsKey(timeZone))
            {
                // If it does not, return UTC offset
                return timeZoneOffset;
            }

            var tzData = _timeZoneService.TimeZones[timeZone];
            timeZoneOffset = enableDst
                ? tzData.Dst
                : tzData.Utc;
            return timeZoneOffset;
        }

        #endregion

        #region Static Methods

        private static IJobController CreateCircleJobController(Instance instance, CircleInstanceType circleInstanceType, List<Coordinate> coords)
        {
            var jobController = new CircleInstanceController(
                instance,
                coords,
                circleInstanceType
            );
            return jobController;
        }

        private static IJobController CreateCircleSmartRaidJobController(IDbContextFactory<MapDataContext> factory, Instance instance, List<Coordinate> coords)
        {
            var jobController = new CircleSmartRaidInstanceController(
                factory,
                instance,
                coords
            );
            return jobController;
        }

        private static IJobController CreateAutoQuestJobController(IDbContextFactory<MapDataContext> mapFactory, IDbContextFactory<DeviceControllerContext> deviceFactory, Instance instance, List<MultiPolygon> multiPolygons, short timeZoneOffset)
        {
            var jobController = new AutoInstanceController(
                mapFactory,
                deviceFactory,
                instance,
                multiPolygons,
                timeZoneOffset
            );
            return jobController;
        }

        private static IJobController CreateIvJobController(Instance instance, List<MultiPolygon> multiPolygons, IvList ivList)
        {
            var jobController = new IvInstanceController(
                instance,
                multiPolygons,
                ivList.PokemonIds
            );
            return jobController;
        }

        #endregion
    }
}