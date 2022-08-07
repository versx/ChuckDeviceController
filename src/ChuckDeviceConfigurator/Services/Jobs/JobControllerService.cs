﻿namespace ChuckDeviceConfigurator.Services.Jobs
{
    using Microsoft.EntityFrameworkCore;
    using POGOProtos.Rpc;

    using ChuckDeviceConfigurator.JobControllers;
    using ChuckDeviceConfigurator.JobControllers.EventArgs;
    using ChuckDeviceConfigurator.Services.Assignments;
    using ChuckDeviceConfigurator.Services.Assignments.EventArgs;
    using ChuckDeviceConfigurator.Services.Geofences;
    using ChuckDeviceConfigurator.Services.IvLists;
    using ChuckDeviceConfigurator.Services.Routing;
    using ChuckDeviceConfigurator.Services.Rpc.Models;
    using ChuckDeviceConfigurator.Services.TimeZone;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;
    using ChuckDeviceController.Plugins;

    // TODO: Refactor class into separate smaller classes
    // TODO: Refactor to accommodate instances/job controllers from plugins

    public class JobControllerService : IJobControllerService
    {
        #region Variables

        private readonly ILogger<IJobControllerService> _logger;
        private readonly IDbContextFactory<ControllerContext> _deviceFactory;
        private readonly IDbContextFactory<MapContext> _mapFactory;
        private readonly ITimeZoneService _timeZoneService;
        private readonly IGeofenceControllerService _geofenceService;
        private readonly IIvListControllerService _ivListService;
        private readonly IAssignmentControllerService _assignmentService;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRouteCalculator _routeCalculator;

        private readonly Dictionary<string, Device> _devices = new();
        private readonly Dictionary<string, IJobController> _instances = new();
        private readonly Dictionary<string, IJobController> _pluginInstances = new();

        private readonly object _devicesLock = new();
        private readonly object _instancesLock = new();
        private readonly object _pluginInstancesLock = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets a dictionary of active and configured devices.
        /// </summary>
        public IReadOnlyDictionary<string, Device> Devices => _devices;

        /// <summary>
        /// Gets a dictionary of all loaded job controller instances.
        /// </summary>
        public IReadOnlyDictionary<string, IJobController> Instances => _instances;

        #endregion

        #region Constructor

        public JobControllerService(
            ILogger<IJobControllerService> logger,
            IDbContextFactory<ControllerContext> deviceFactory,
            IDbContextFactory<MapContext> mapFactory,
            ITimeZoneService timeZoneService,
            IGeofenceControllerService geofenceService,
            IIvListControllerService ivListService,
            IRouteGenerator routeGenerator,
            IRouteCalculator routeCalculator,
            IAssignmentControllerService assignmentService)
        {
            _logger = logger;
            _deviceFactory = deviceFactory;
            _mapFactory = mapFactory;
            _timeZoneService = timeZoneService;
            _geofenceService = geofenceService;
            _ivListService = ivListService;
            _routeGenerator = routeGenerator;
            _routeCalculator = routeCalculator;
            _assignmentService = assignmentService;
            _assignmentService.DeviceReloaded += OnAssignmentDeviceReloaded;
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
                    var callback = new WaitCallback(async _ =>
                    {
                        _logger.LogInformation($"Starting instance {instance.Name}");
                        await AddInstanceAsync(instance);
                        _logger.LogInformation($"Started instance {instance.Name}");

                        var newDevices = devices.AsEnumerable()
                                                .Where(device => string.Compare(device.InstanceName, instance.Name, true) == 0);
                        foreach (var device in newDevices)
                        {
                            AddDevice(device);
                        }
                    });

                    if (!ThreadPool.QueueUserWorkItem(callback))
                    {
                        _logger.LogError($"Failed to start instance {instance.Name}");
                    }
                }
            }
            _logger.LogInformation("All instances have been started");
        }

        public async void Stop()
        {
            foreach (var (name, jobController) in _instances)
            {
                _logger.LogInformation($"[{name}] Stopping job controller");
                await jobController.StopAsync();
            }
        }

        #endregion

        #region Plugin Host

        public async Task CreateInstanceTypeAsync(IInstanceCreationOptions options)
        {
            // TODO: Allow plugins to create instances to link with job controllers, that way they are easily used via the UI
            var instance = new Instance
            {
                Name = options.Name,
                MinimumLevel = options.MinimumLevel,
                MaximumLevel = options.MaximumLevel,
                // TODO: When InstanceType.Custom selected via UI - maybe show a separate select listing available job controllers from plugins (add InstanceData property for 'custom_instance_name' or something)
                Type = InstanceType.Custom,
                Geofences = options.Geofences,
                Data = new InstanceData
                {
                    AccountGroup = options.GroupName,
                    IsEvent = options.IsEvent,
                    // TODO: Allow for custom instance data properties
                },
            };

            // TODO: Add to database?
            // TODO: Allow geofence creation/assignment?

            await AddInstanceAsync(instance);
        }

        public async Task AddJobControllerAsync(string name, IJobController jobController)
        {
            if (string.IsNullOrEmpty(name))
            {
                _logger.LogError($"Job controller name cannot be null, skipping...");
                return;
            }

            if (jobController == null)
            {
                _logger.LogError($"[{name}] Unable to instantiate job instance controller with instance name '{name}'");
                return;
            }

            lock (_pluginInstancesLock)
            {
                if (_pluginInstances.ContainsKey(name))
                {
                    _logger.LogError($"[{name}] Job controller instance with name '{name}' already exists, unable to add job controller");
                    return;
                }
                _pluginInstances.Add(name, jobController);

                _logger.LogInformation($"Successfully added job controller '{name}' to plugin job controllers cache from plugin");
            }

            await Task.CompletedTask;
        }

        public async Task AssignDeviceToJobControllerAsync(IDevice device, string jobControllerName)
        {
            lock (_pluginInstancesLock)
            {
                if (!_pluginInstances.ContainsKey(jobControllerName))
                {
                    _logger.LogError($"Job controller instance with name '{jobControllerName}' does not exist, unable to assign device '{device.Uuid}'. Make sure you add the job controller first before assigning devices to it.");
                    return;
                }

                if (!_devices.ContainsKey(device.Uuid))
                {
                    _logger.LogError($"Device with name '{device.Uuid}' does not exist, unable to assign job controller instance");
                    return;
                }
            }
            //var jobController = _pluginInstances[jobControllerName];

            // Assign device to plugin job controller instance name
            await AssignDevice((Device)device, jobControllerName);

            // Reload device to take new job controller instance assignment in effect
            ReloadDevice((Device)device, device.Uuid);
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

            var geofences = _geofenceService.GetByNames(instance.Geofences);
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
                    }
                    break;
                case InstanceType.AutoQuest:
                case InstanceType.Bootstrap:
                case InstanceType.DynamicPokemon:
                case InstanceType.FindTth:
                case InstanceType.Leveling:
                case InstanceType.PokemonIV:
                case InstanceType.SmartRaid:
                    var (multiPolygons, coordinates) = geofences.ConvertToMultiPolygons();
                    switch (instance.Type)
                    {
                        case InstanceType.AutoQuest:
                            var timeZone = instance.Data?.TimeZone;
                            var timeZoneOffset = ConvertTimeZoneToOffset(timeZone, instance.Data?.EnableDst ?? Strings.DefaultEnableDst);
                            jobController = CreateAutoQuestJobController(_mapFactory, _deviceFactory, instance, multiPolygons, timeZoneOffset);
                            ((AutoInstanceController)jobController).InstanceComplete += OnAutoInstanceComplete;
                            break;
                        case InstanceType.Bootstrap:
                            jobController = CreateBootstrapJobController(instance, multiPolygons, _routeGenerator, _routeCalculator);
                            ((BootstrapInstanceController)jobController).InstanceComplete += OnBootstrapInstanceComplete;
                            break;
                        case InstanceType.DynamicPokemon:
                            jobController = CreateDynamicJobController(instance, multiPolygons, _routeGenerator, _routeCalculator);
                            break;
                        case InstanceType.FindTth:
                            jobController = CreateSpawnpointJobController(_mapFactory, instance, multiPolygons, _routeCalculator);
                            break;
                        case InstanceType.Leveling:
                            jobController = CreateLevelingJobController(_deviceFactory, instance, multiPolygons);
                            ((LevelingInstanceController)jobController).AccountLevelUp += OnAccountLevelUp;
                            break;
                        case InstanceType.PokemonIV:
                            var ivList = _ivListService.GetByName(instance.Data?.IvList ?? Strings.DefaultIvList);
                            if (ivList == null)
                            {
                                _logger.LogError($"Failed to fetch IV list for instance {instance.Name}, skipping controller instantiation...");
                                return;
                            }
                            jobController = CreateIvJobController(_mapFactory, instance, multiPolygons, ivList);
                            break;
                        case InstanceType.SmartRaid:
                            jobController = CreateSmartRaidJobController(_mapFactory, instance, multiPolygons);
                            break;
                    }
                    break;
                case InstanceType.Custom:
                    // TODO: Use for job controllers created by plugins
                    if (!_pluginInstances.ContainsKey(instance.Name))
                    {
                        _logger.LogError($"[{instance.Name}] Plugin job controller has not been registered, unable to initialize job controller instance");
                        return;
                    }
                    jobController = _pluginInstances[instance.Name];
                    break;
            }

            if (jobController == null)
            {
                _logger.LogError($"[{instance.Name}] Unable to instantiate job instance controller with instance name '{instance.Name}' and type '{instance.Type}'");
                return;
            }

            if (instance.Type == InstanceType.Custom)
            {
                lock (_pluginInstancesLock)
                {
                    _pluginInstances[instance.Name] = jobController;
                }
            }
            else
            {
                lock (_instancesLock)
                {
                    _instances[instance.Name] = jobController;
                }
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
                var instanceName = device.InstanceName;
                if (device == null || string.IsNullOrEmpty(instanceName))
                {
                    _logger.LogWarning($"Device or device instance name was null, unable to retrieve job controller instance");
                    return null;
                }

                return GetInstanceControllerByName(instanceName);
            }
        }

        public IJobController GetInstanceControllerByName(string instanceName)
        {
            IJobController jobController;
            lock (_instancesLock)
            {
                if (!_instances.ContainsKey(instanceName))
                {
                    _logger.LogError($"[{instanceName}] Unable to get instance controller by name, it does not exist in cache");
                    return null;
                }
                jobController = _instances[instanceName];
            }
            return jobController;
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
                    instanceController?.ReloadAsync().ConfigureAwait(false);
                }
            }
            _assignmentService.Reload();
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
                    _instances[oldInstanceName]?.StopAsync().ConfigureAwait(false);
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
                _instances[instanceName]?.StopAsync();
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

            _assignmentService.Reload();
            await Task.CompletedTask;
        }

        #endregion

        #region IV Queue

        public IReadOnlyList<Pokemon> GetIvQueue(string instanceName)
        {
            var queue = new List<Pokemon>();
            lock (_instancesLock)
            {
                // Check if instance exists in cache by name, if not return empty queue.
                if (!_instances.ContainsKey(instanceName))
                    return queue;

                var instance = _instances[instanceName];
                if (instance is IvInstanceController iv)
                {
                    queue = (List<Pokemon>)iv.GetQueue();
                }
            }
            return queue;
        }

        public void RemoveFromIvQueue(string instanceName, string encounterId)
        {
            lock (_instancesLock)
            {
                var jobController = GetInstanceControllerByName(instanceName);
                if (jobController is IvInstanceController ivController)
                {
                    ivController.RemoveFromQueue(encounterId);
                }
            }
        }

        public void ClearIvQueue(string instanceName)
        {
            lock (_instancesLock)
            {
                var jobController = GetInstanceControllerByName(instanceName);
                if (jobController is IvInstanceController ivController)
                {
                    ivController.ClearQueue();
                }
            }
        }

        #endregion

        #region Quest Queue

        public IReadOnlyList<PokestopWithMode> GetQuestQueue(string instanceName)
        {
            var queue = new List<PokestopWithMode>();
            lock (_instancesLock)
            {
                // Check if instance exists in cache by name, if not return empty queue.
                if (!_instances.ContainsKey(instanceName))
                    return queue;

                var instance = _instances[instanceName];
                if (instance is AutoInstanceController quest)
                {
                    queue = (List<PokestopWithMode>)quest.GetQueue();
                }
            }
            return queue;
        }

        #endregion

        #region Receivers

        public void GotPokemon(Pokemon pokemon, bool hasIv)
        {
            lock (_instancesLock)
            {
                foreach (var (_, jobController) in _instances)
                {
                    if (jobController is IvInstanceController ivController)
                    {
                        ivController.GotPokemon(pokemon, hasIv);
                    }
                }
            }
        }

        public void GotFort(PokemonFortProto fort, string username)
        {
            lock (_instancesLock)
            {
                foreach (var (_, jobController) in _instances)
                {
                    if (jobController is LevelingInstanceController levelController)
                    {
                        levelController.GotFort(fort, username);
                    }
                }
            }
        }

        public void GotPlayerInfo(string username, ushort level, ulong xp)
        {
            lock (_instancesLock)
            {
                foreach (var (_, jobController) in _instances)
                {
                    if (jobController is LevelingInstanceController levelController)
                    {
                        levelController.SetPlayerInfo(username, level, xp);
                    }
                }
            }
        }

        public TrainerLevelingStatus GetTrainerLevelingStatus(string username)
        {
            var storeLevelingData = false;
            var isTrainerLeveling = false;

            lock (_instancesLock)
            {
                foreach (var (_, jobController) in _instances)
                {
                    if (jobController is not LevelingInstanceController levelController)
                        continue;

                    // Check if trainer account is using leveling instance
                    if (levelController.HasTrainer(username))
                    {
                        storeLevelingData = levelController.StoreLevelData;
                        isTrainerLeveling = true;
                        break;
                    }
                }
            }

            return new TrainerLevelingStatus
            (
                username,
                isTrainerLeveling,
                storeLevelingData
            );
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
            _assignmentService.Reload();
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

            _assignmentService.Reload();
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
            _assignmentService.Reload();
        }

        #endregion

        #region Job Controller Event Handlers

        private async void OnBootstrapInstanceComplete(object? sender, BootstrapInstanceCompleteEventArgs e)
        {
            if (!_devices.ContainsKey(e.DeviceUuid))
            {
                _logger.LogWarning($"[{e.DeviceUuid}] Device does not exist in device list");
                return;
            }

            _logger.LogInformation($"[{e.DeviceUuid}] Device finished bootstrapping, switching to chained instance {e.InstanceName}");

            var device = _devices[e.DeviceUuid];
            device.InstanceName = e.InstanceName;
            using (var context = _deviceFactory.CreateDbContext())
            {
                context.Update(device);
                await context.SaveChangesAsync();
            }
            ReloadDevice(device, e.DeviceUuid);
        }

        private async void OnAutoInstanceComplete(object? sender, AutoInstanceCompleteEventArgs e)
        {
            await _assignmentService.InstanceControllerCompleteAsync(e.InstanceName);
        }

        private void OnAssignmentDeviceReloaded(object? sender, AssignmentDeviceReloadedEventArgs e)
        {
            ReloadDevice(e.Device, e.Device.Uuid);
        }

        private void OnAccountLevelUp(object? sender, AccountLevelUpEventArgs e)
        {
            var date = e.DateReached.FromSeconds()
                                    .ToLocalTime();
            _logger.LogInformation($"Account {e.Username} has reached level {e.Level} at {date} with a total of {e.XP} XP!");
        }

        #endregion

        #region Private Methods

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
                // If it does not, return UTC+0 offset
                return timeZoneOffset;
            }

            var tzData = _timeZoneService.TimeZones[timeZone];
            timeZoneOffset = enableDst
                // Return DST offset for time zone
                ? tzData.Dst
                // Return non-DST offset for time zone
                : tzData.Utc;
            return timeZoneOffset;
        }

        private async Task AssignDevice(Device device, string instanceName)
        {
            using (var context = _deviceFactory.CreateDbContext())
            {
                device.InstanceName = instanceName;
                context.Devices.Update(device);
                await context.SaveChangesAsync();
            }
        }

        #endregion

        #region Job Controller Methods

        private static IJobController CreateCircleJobController(Instance instance, CircleInstanceType circleInstanceType, List<Coordinate> coords)
        {
            var jobController = new CircleInstanceController(
                instance,
                coords,
                circleInstanceType
            );
            return jobController;
        }

        private static IJobController CreateSmartRaidJobController(IDbContextFactory<MapContext> factory, Instance instance, List<MultiPolygon> multiPolygons)
        {
            var jobController = new SmartRaidInstanceController(
                factory,
                instance,
                multiPolygons
            );
            return jobController;
        }

        private static IJobController CreateAutoQuestJobController(IDbContextFactory<MapContext> mapFactory, IDbContextFactory<ControllerContext> deviceFactory, Instance instance, List<MultiPolygon> multiPolygons, short timeZoneOffset)
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

        private static IJobController CreateBootstrapJobController(Instance instance, List<MultiPolygon> multiPolygons, IRouteGenerator routeGenerator, IRouteCalculator routeCalculator)
        {
            var jobController = new BootstrapInstanceController(
                instance,
                multiPolygons,
                routeGenerator,
                routeCalculator
            );
            return jobController;
        }

        private static IJobController CreateDynamicJobController(Instance instance, List<MultiPolygon> multiPolygons, IRouteGenerator routeGenerator, IRouteCalculator routeCalculator)
        {
            var jobController = new DynamicRouteInstanceController(
                instance,
                multiPolygons,
                routeGenerator,
                routeCalculator
            );
            return jobController;
        }

        private static IJobController CreateIvJobController(IDbContextFactory<MapContext> mapFactory, Instance instance, List<MultiPolygon> multiPolygons, IvList ivList)
        {
            var jobController = new IvInstanceController(
                mapFactory,
                instance,
                multiPolygons,
                ivList.PokemonIds
            );
            return jobController;
        }

        private static IJobController CreateLevelingJobController(IDbContextFactory<ControllerContext> deviceFactory, Instance instance, List<MultiPolygon> multiPolygons)
        {
            var jobController = new LevelingInstanceController(
                deviceFactory,
                instance,
                multiPolygons
            );
            return jobController;
        }

        private static IJobController CreateSpawnpointJobController(IDbContextFactory<MapContext> mapFactory, Instance instance, List<MultiPolygon> multiPolygons, IRouteCalculator routeCalculator)
        {
            var jobController = new TthFinderInstanceController(
                mapFactory,
                instance,
                multiPolygons,
                routeCalculator
            );
            return jobController;
        }

        #endregion
    }
}