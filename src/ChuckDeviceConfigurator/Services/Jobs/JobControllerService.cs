namespace ChuckDeviceConfigurator.Services.Jobs
{
    using ChuckDeviceConfigurator.JobControllers;
    using ChuckDeviceConfigurator.Services.TimeZone;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    using Microsoft.EntityFrameworkCore;

    // TODO: HostedService?
    public class JobControllerService : IJobControllerService
    {
        #region Variables

        private readonly ILogger<IJobControllerService> _logger;
        private readonly IDbContextFactory<DeviceControllerContext> _deviceFactory;
        private readonly IDbContextFactory<MapDataContext> _mapFactory;
        private readonly IConfiguration _configuration;
        private readonly ITimeZoneService _timeZoneService;

        private readonly IDictionary<string, Device> _devices;
        private readonly IDictionary<string, IJobController> _instances;

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

        public JobControllerService(
            ILogger<IJobControllerService> logger,
            IDbContextFactory<DeviceControllerContext> deviceFactory,
            IDbContextFactory<MapDataContext> mapFactory,
            IConfiguration configuration,
            ITimeZoneService timeZoneService)
        {
            _logger = logger;
            _deviceFactory = deviceFactory;
            _mapFactory = mapFactory;
            _configuration = configuration;
            _timeZoneService = timeZoneService;

            _devices = new Dictionary<string, Device>();
            _instances = new Dictionary<string, IJobController>();

            //Start();
        }

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

        #region Instances

        public async Task AddInstanceAsync(Instance instance)
        {
            _logger.LogDebug($"Adding instance {instance.Name}");

            var geofences = GetGeofences(instance.Geofences);
            if (geofences == null)
            {
                _logger.LogError($"[{instance.Name}] Failed to get geofences for instance, make sure it is assigned at least one");
                return;
            }

            IJobController? jobController = null;
            switch (instance.Type)
            {
                case InstanceType.CirclePokemon:
                case InstanceType.CircleSmartPokemon:
                case InstanceType.CircleRaid:
                case InstanceType.CircleSmartRaid:
                    var coords = new List<Coordinate>();
                    foreach (var geofence in geofences)
                    {
                        var area = geofence.Data?.Area;
                        if (area is null)
                        {
                            _logger.LogError($"[{instance.Name}] Failed to parse geofence '{geofence.Name}' coordinates");
                            continue;
                        }
                        string areaJson = Convert.ToString(area);
                        var coordsArray = (List<Coordinate>)
                        (
                            area is List<Coordinate>
                                ? area
                                : areaJson.FromJson<List<Coordinate>>()
                        );
                        coords.AddRange(coordsArray);
                    }

                    switch (instance.Type)
                    {
                        case InstanceType.CirclePokemon:
                            jobController = new CircleInstanceController(instance, coords, CircleInstanceType.Pokemon);
                            break;
                        case InstanceType.CircleSmartPokemon:
                            break;
                        case InstanceType.CircleRaid:
                            jobController = new CircleInstanceController(instance, coords, CircleInstanceType.Raid);
                            break;
                        case InstanceType.CircleSmartRaid:
                            break;
                    }
                    break;
                case InstanceType.AutoQuest:
                case InstanceType.PokemonIV:
                case InstanceType.Bootstrap:
                case InstanceType.FindTth:
                    var multiPolygons = new List<MultiPolygon>();
                    var coordinates = new List<List<Coordinate>>();
                    foreach (var geofence in geofences)
                    {
                        var area = geofence.Data?.Area;
                        if (area is null)
                        {
                            _logger.LogError($"[{instance.Name}] Failed to parse coordinates for geofence '{geofence.Name}'");
                            continue;
                        }
                        string areaJson = Convert.ToString(area);
                        var coordsArray = (List<List<Coordinate>>)
                        (
                            area is List<List<Coordinate>>
                                ? area
                                : areaJson.FromJson<List<List<Coordinate>>>()
                        );
                        coordinates.AddRange(coordsArray);

                        var areaArrayEmptyInner = new List<MultiPolygon>();
                        foreach (var coord in coordsArray)
                        {
                            var multiPolygon = new MultiPolygon();
                            Coordinate? first = null;
                            for (var i = 0; i < coord.Count; i++)
                            {
                                if (i == 0)
                                {
                                    first = coord[i];
                                }
                                multiPolygon.Add(new Polygon(coord[i].Latitude, coord[i].Longitude));
                            }
                            if (first != null)
                            {
                                multiPolygon.Add(new Polygon(first.Latitude, first.Longitude));
                            }
                            areaArrayEmptyInner.Add(multiPolygon);
                        }
                        multiPolygons.AddRange(areaArrayEmptyInner);
                    }

                    switch (instance.Type)
                    {
                        case InstanceType.AutoQuest:
                            var timezone = instance.Data?.TimeZone;
                            short timezoneOffset = 0;
                            if (!string.IsNullOrEmpty(timezone) && _timeZoneService.TimeZones.ContainsKey(timezone))
                            {
                                var tzData = _timeZoneService.TimeZones[timezone];
                                timezoneOffset = instance.Data?.EnableDst ?? false
                                    ? tzData.Dst
                                    : tzData.Utc;
                                timezoneOffset *= 3600;
                            }
                            jobController = new AutoInstanceController(_mapFactory, _deviceFactory, instance, multiPolygons, timezoneOffset);
                            break;
                        case InstanceType.Bootstrap:
                            jobController = new BootstrapInstanceController(instance, coordinates);
                            break;
                        case InstanceType.FindTth:
                            jobController = new TthFinderInstanceController(instance, coordinates);
                            break;
                        case InstanceType.PokemonIV:
                            // TODO: Get IvList from IvListController.Instance
                            //jobController = new IvInstanceController(instance, multiPolygons, ivList);
                            break;
                    }
                    break;
            }

            if (jobController == null)
            {
                _logger.LogError($"[{instance.Name}] Unable to instantiate job instance controller with instance type '{instance.Type}'");
                return;
            }

            if (string.IsNullOrEmpty(instance.Name))
            {
                return;
            }

            // TODO: Lock
            _instances[instance.Name] = jobController;

            await Task.CompletedTask;
        }

        public IJobController GetInstanceController(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                // TODO Log error
                return null;
            }

            // TODO: Lock _devices
            if (!_devices.ContainsKey(uuid))
            {
                _logger.LogWarning($"[{uuid}] Device is not assigned an instance!");
                return null;
            }

            var device = _devices[uuid];
            var instanceName = device?.InstanceName;
            if (device == null || string.IsNullOrEmpty(instanceName))
            {
                return null;
            }

            return GetInstanceControllerByName(instanceName);
        }

        public async Task<string> GetStatusAsync(Instance instance)
        {
            if (!_instances.ContainsKey(instance.Name))
            {
                // Instance not started or added to instance cache yet
                return "Starting...";
            }

            var instanceController = _instances[instance.Name];
            if (instanceController != null)
            {
                return await instanceController.GetStatusAsync();
            }

            return "Error";
        }

        public void ReloadAll()
        {
            // TODO: Lock _instances
            foreach (var (_, instanceController) in _instances)
            {
                instanceController?.Reload();
            }
        }

        public async Task ReloadInstanceAsync(Instance newInstance, string oldInstanceName)
        {
            // TODO: Lock _instances
            if (!_instances.ContainsKey(oldInstanceName))
            {
                _logger.LogError($"[{oldInstanceName}] Instance does not exist in instance cache, skipping instance reload...");
                return;
            }

            var oldInstance = _instances[oldInstanceName];
            if (oldInstance != null)
            {
                foreach (var (uuid, device) in _devices)
                {
                    if (string.Compare(device.InstanceName, oldInstance.Name, true) == 0)
                    {
                        device.InstanceName = newInstance.Name;
                        _devices[uuid] = device;
                    }
                }
            }

            await RemoveInstanceAsync(oldInstanceName);
            await AddInstanceAsync(newInstance);
        }

        public async Task RemoveInstanceAsync(string instanceName)
        {
            // TODO: Lock
            _instances[instanceName]?.Stop();
            _instances[instanceName] = null;
            _instances.Remove(instanceName);

            var devices = _devices.Where(device => string.Compare(device.Value.InstanceName, instanceName, true) == 0);
            foreach (var device in devices)
            {
                _devices[device.Key] = null;
            }

            // TODO: await _assignmentController.Start();
            await Task.CompletedTask;
        }

        #endregion

        #region Devices

        public void AddDevice(Device device)
        {
            // TODO: Lock _devices
            if (!_devices.ContainsKey(device.Uuid))
            {
                _devices.Add(device.Uuid, device);
            }
        }

        public List<string> GetDeviceUuidsInInstance(string instanceName)
        {
            // TODO: Lock _devices
            var uuids = new List<string>();
            foreach (var (uuid, device) in _devices)
            {
                if (string.Compare(device.InstanceName, instanceName, true) == 0)
                {
                    uuids.Add(uuid);
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
            // TODO: await _assignmentController.Start();
            await Task.CompletedTask;
        }

        public void RemoveDevice(string uuid)
        {
            // TODO: Lock _devices
            if (!_devices.ContainsKey(uuid))
            {
                _logger.LogError($"[{uuid}] Unable to remove device from cache, it does not exist");
                return;
            }
            _devices.Remove(uuid);
        }

        #endregion

        #region Private Methods

        private IJobController GetInstanceControllerByName(string name)
        {
            // TODO: Lock _instances
            if (!_instances.ContainsKey(name))
            {
                _logger.LogError($"[{name}] Unable to get instance controller by name, it does not exist in cache");
                return null;
            }
            return _instances[name];
        }

        private List<Geofence> GetGeofences(List<string> names)
        {
            // TODO: Add GeofenceControllerService
            using (var context = _deviceFactory.CreateDbContext())
            {
                return context.Geofences.Where(geofence => names.Contains(geofence.Name))
                                        .ToList();
            }
        }

        #endregion
    }
}