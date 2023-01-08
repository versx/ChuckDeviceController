namespace ChuckDeviceConfigurator.Services.Jobs;

using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using POGOProtos.Rpc;

using ChuckDeviceConfigurator.Extensions;
using ChuckDeviceConfigurator.Services.Assignments;
using ChuckDeviceConfigurator.Services.Assignments.EventArgs;
using ChuckDeviceConfigurator.Services.Geofences;
using ChuckDeviceConfigurator.Services.IvLists;
using ChuckDeviceConfigurator.Services.Rpc.Models;
using ChuckDeviceConfigurator.Services.TimeZone;
using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Geometry.Models.Abstractions;
using ChuckDeviceController.JobControllers;
using ChuckDeviceController.Logging;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.PluginManager;
using ChuckDeviceController.Routing;

using Type = System.Type;

// TODO: Refactor class into separate smaller classes
// TODO: Create MySqlConnectionFactory to replace DeviceFactory and MapFactory

public class JobControllerService : IJobControllerService
{
    #region Variables

    private static readonly ILogger<IJobControllerService> _logger =
        GenericLoggerFactory.CreateLogger<IJobControllerService>();
    private readonly IDapperUnitOfWork _uow;
    private readonly IDbContextFactory<ControllerDbContext> _deviceFactory;
    private readonly IDbContextFactory<MapDbContext> _mapFactory;
    private readonly ITimeZoneService _timeZoneService;
    private readonly IGeofenceControllerService _geofenceService;
    private readonly IIvListControllerService _ivListService;
    private readonly IAssignmentControllerService _assignmentService = null!;
    private readonly IRoutingHost _routeGenerator;
    private readonly IRouteCalculator _routeCalculator;
    private ServiceProvider _serviceProvider = null!;

    private static readonly ConcurrentDictionary<string, IDevice> _devices = new();
    private static readonly ConcurrentDictionary<string, IJobController> _instances = new();
    private static readonly ConcurrentDictionary<string, Type> _pluginInstances = new();

    #endregion

    #region Properties

    /// <summary>
    /// Gets a dictionary of active and configured devices.
    /// </summary>
    public IReadOnlyDictionary<string, IDevice> Devices => _devices;

    /// <summary>
    /// Gets a dictionary of all loaded job controller instances.
    /// </summary>
    public IReadOnlyDictionary<string, IJobController> Instances => _instances;

    /// <summary>
    /// Gets a dictionary of all registered custom job controller instance types.
    /// </summary>
    public IReadOnlyDictionary<string, GeofenceType> CustomInstanceTypes =>
        _pluginInstances.ToDictionary(x => x.Key, pair =>
            pair.Value.GetCustomAttribute<GeofenceTypeAttribute>()?.Type ?? GeofenceType.Geofence);

    public IServiceProvider Services { get; }

    #endregion

    #region Constructor

    public JobControllerService(
        IDapperUnitOfWork uow,
        IDbContextFactory<ControllerDbContext> deviceFactory,
        IDbContextFactory<MapDbContext> mapFactory,
        ITimeZoneService timeZoneService,
        IGeofenceControllerService geofenceService,
        IIvListControllerService ivListService,
        IRoutingHost routeGenerator,
        IRouteCalculator routeCalculator,
        IAssignmentControllerService assignmentService,
        IServiceProvider services)
    {
        _uow = uow;
        _deviceFactory = deviceFactory;
        _mapFactory = mapFactory;
        _timeZoneService = timeZoneService;
        _geofenceService = geofenceService;
        _ivListService = ivListService;
        _routeGenerator = routeGenerator;
        _routeCalculator = routeCalculator;
        _assignmentService = assignmentService;
        if (_assignmentService != null)
        {
            _assignmentService.DeviceReloaded += OnAssignmentDeviceReloaded;
            _assignmentService.ReloadInstance += OnReloadInstance;
        }

        Services = services;
    }

    #endregion

    #region Public Methods

    public async void LoadDevices(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        using var scope = Services.CreateScope();
        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var devices = await uow.Devices.FindAllAsync();
        devices.ToList().ForEach(AddDevice);
    }

    public async void Start()
    {
        using var scope = Services.CreateScope();
        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var instances = await uow.Instances.FindAllAsync();
        var devices = _devices.Values;

        foreach (var instance in instances)
        {
            var callback = new WaitCallback(async _ =>
            {
                _logger.LogInformation($"Starting instance {instance.Name} ({instance.Type})");
                await AddInstanceAsync(instance);

                var deviceCount = devices.Count(device => device.InstanceName == instance.Name);
                var suffix = deviceCount > 0
                    ? $", now loading {deviceCount:N0} assigned devices."
                    : "";
                var instanceType = instance.Type == InstanceType.Custom
                    ? $"{instance.Type} - {instance.Data?.CustomInstanceType}"
                    : instance.Type.ToString();
                _logger.LogInformation($"Started instance {instance.Name} ({instanceType}){suffix}");
            });

            if (!ThreadPool.QueueUserWorkItem(callback))
            {
                _logger.LogError($"Failed to start instance {instance.Name} ({instance.Type})");
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

    /// <summary>
    /// Creates a new instance provided by a plugin.
    /// </summary>
    /// <param name="options">Instance option used when creating the instance.</param>
    public async Task CreateInstanceAsync(IInstance options)
    {
        // Allow plugins to create instances to link with job controllers, that way they are easily used via the UI
        var instance = new Instance
        {
            Name = options.Name,
            Type = InstanceType.Custom,
            MinimumLevel = options.MinimumLevel,
            MaximumLevel = options.MaximumLevel,
            Geofences = options.Geofences,
            Data = options.Data,
        };

        using var scope = Services.CreateScope();
        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        if (uow.Instances.Any(x => x.Name == options.Name))
        {
            await uow.Instances.UpdateAsync(instance);
        }
        else
        {
            await uow.Instances.AddAsync(instance);
        }
        await uow.CommitAsync();

        await AddInstanceAsync(instance);
    }

    /// <summary>
    /// Registers any custom job controller instance types provided by plugins.
    /// </summary>
    /// <typeparam name="T">New job controller instance type to register.</typeparam>
    /// <param name="customInstanceType">Name describing the new job controller instance type.</param>
    public async Task RegisterJobControllerAsync<T>(string customInstanceType)
        where T : IJobController
    {
        if (string.IsNullOrEmpty(customInstanceType))
        {
            _logger.LogError($"Job controller type cannot be null, skipping job controller registration...");
            return;
        }

        if (!_pluginInstances.ContainsKey(customInstanceType))
        {
            if (!_pluginInstances.TryAdd(customInstanceType, typeof(T)))
            {
                // Failed to register job controller instance from plugin
                _logger.LogError($"Failed to register job controller instance from plugin: {customInstanceType}");
                return;
            }
            _logger.LogInformation($"Successfully added job controller '{customInstanceType}' to plugin job controllers cache from plugin");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Assigns a device to the specified instance by name.
    /// </summary>
    /// <param name="device">Device to assign the instance to.</param>
    /// <param name="instanceName">Instance name that will be assigned to the device.</param>
    public async Task AssignDeviceToJobControllerAsync(IDevice device, string instanceName)
    {
        // Ensure our specified device has already been registered by the job controller service.
        if (!_devices.ContainsKey(device.Uuid))
        {
            _logger.LogError($"Device with name '{device.Uuid}' does not exist, unable to assign job controller instance");
            return;
        }

        // Assign device to plugin job controller instance name
        await AssignDevice((Device)device, instanceName);

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
        if (instance.Type == InstanceType.CirclePokemon ||
            instance.Type == InstanceType.CircleRaid)
        {
            var coords = geofences.ConvertToCoordinates();
            var circleInstanceType = instance.Type == InstanceType.CirclePokemon
                ? CircleInstanceType.Pokemon
                : CircleInstanceType.Raid;
            jobController = CreateCircleJobController(instance, circleInstanceType, coords);
        }
        else if (instance.Type == InstanceType.Custom)
        {
            //var customInstanceType = Convert.ToString(instance.Data?["custom_instance_type"]);
            var customInstanceType = instance.Data?.CustomInstanceType;
            if (string.IsNullOrEmpty(customInstanceType))
            {
                _logger.LogError($"[{instance.Name}] Plugin job controller instance type is not set, unable to initialize job controller instance");
                return;
            }
            jobController = CreatePluginJobController(customInstanceType, instance, geofences, _serviceProvider);
        }
        else
        {
            var (multiPolygons, coordinates) = geofences.ConvertToMultiPolygons();
            if (instance.Type == InstanceType.AutoQuest)
            {
                var timeZone = instance.Data?.TimeZone;
                var timeZoneOffset = ConvertTimeZoneToOffset(timeZone, instance.Data?.EnableDst ?? Strings.DefaultEnableDst);
                jobController = CreateAutoQuestJobController(_uow, _mapFactory, _deviceFactory, instance, multiPolygons, timeZoneOffset);
                ((AutoInstanceController)jobController).InstanceComplete += OnAutoInstanceComplete;
            }
            else if (instance.Type == InstanceType.Bootstrap)
            {
                jobController = CreateBootstrapJobController(instance, multiPolygons, _routeGenerator, _routeCalculator);
                ((BootstrapInstanceController)jobController).InstanceComplete += OnBootstrapInstanceComplete;
            }
            else if (instance.Type == InstanceType.DynamicRoute)
            {
                jobController = CreateDynamicJobController(instance, multiPolygons, _routeGenerator, _routeCalculator);
            }
            else if (instance.Type == InstanceType.FindTth)
            {
                jobController = CreateSpawnpointJobController(_uow, instance, multiPolygons, _routeCalculator);
            }
            else if (instance.Type == InstanceType.Leveling)
            {
                jobController = CreateLevelingJobController(_uow, instance, multiPolygons);
                ((LevelingInstanceController)jobController).AccountLevelUp += OnAccountLevelUp;
            }
            else if (instance.Type == InstanceType.PokemonIV)
            {
                var ivList = _ivListService.GetByName(instance.Data?.IvList ?? Strings.DefaultIvList);
                if (ivList == null)
                {
                    _logger.LogError($"[{instance.Name}] Failed to fetch IV list, skipping controller instantiation...");
                    return;
                }
                jobController = CreateIvJobController(_uow, instance, multiPolygons, ivList);
            }
            else if (instance.Type == InstanceType.SmartRaid)
            {
                jobController = CreateSmartRaidJobController(_uow, instance, multiPolygons);
            }
        }

        //switch (instance.Type)
        //{
        //    case InstanceType.CirclePokemon:
        //    case InstanceType.CircleRaid:
        //        var coords = geofences.ConvertToCoordinates();
        //        var circleInstanceType = instance.Type == InstanceType.CirclePokemon
        //            ? CircleInstanceType.Pokemon
        //            : CircleInstanceType.Raid;
        //        jobController = CreateCircleJobController(instance, circleInstanceType, coords);
        //        break;
        //    case InstanceType.AutoQuest:
        //    case InstanceType.Bootstrap:
        //    case InstanceType.DynamicRoute:
        //    case InstanceType.FindTth:
        //    case InstanceType.Leveling:
        //    case InstanceType.PokemonIV:
        //    case InstanceType.SmartRaid:
        //        var (multiPolygons, coordinates) = geofences.ConvertToMultiPolygons();
        //        switch (instance.Type)
        //        {
        //            case InstanceType.AutoQuest:
        //                var timeZone = instance.Data?.TimeZone;
        //                var timeZoneOffset = ConvertTimeZoneToOffset(timeZone, instance.Data?.EnableDst ?? Strings.DefaultEnableDst);
        //                jobController = CreateAutoQuestJobController(_factory, _mapFactory, _deviceFactory, instance, multiPolygons, timeZoneOffset);
        //                ((AutoInstanceController)jobController).InstanceComplete += OnAutoInstanceComplete;
        //                break;
        //            case InstanceType.Bootstrap:
        //                jobController = CreateBootstrapJobController(instance, multiPolygons, _routeGenerator, _routeCalculator);
        //                ((BootstrapInstanceController)jobController).InstanceComplete += OnBootstrapInstanceComplete;
        //                break;
        //            case InstanceType.DynamicRoute:
        //                jobController = CreateDynamicJobController(instance, multiPolygons, _routeGenerator, _routeCalculator);
        //                break;
        //            case InstanceType.FindTth:
        //                jobController = CreateSpawnpointJobController(_mapFactory, instance, multiPolygons, _routeCalculator);
        //                break;
        //            case InstanceType.Leveling:
        //                jobController = CreateLevelingJobController(_deviceFactory, instance, multiPolygons);
        //                ((LevelingInstanceController)jobController).AccountLevelUp += OnAccountLevelUp;
        //                break;
        //            case InstanceType.PokemonIV:
        //                var ivList = _ivListService.GetByName(instance.Data?.IvList ?? Strings.DefaultIvList);
        //                if (ivList == null)
        //                {
        //                    _logger.LogError($"[{instance.Name}] Failed to fetch IV list, skipping controller instantiation...");
        //                    return;
        //                }
        //                jobController = CreateIvJobController(_mapFactory, instance, multiPolygons, ivList);
        //                break;
        //            case InstanceType.SmartRaid:
        //                jobController = CreateSmartRaidJobController(_mapFactory, instance, multiPolygons);
        //                break;
        //        }
        //        break;
        //    case InstanceType.Custom:
        //        //var customInstanceType = Convert.ToString(instance.Data?["custom_instance_type"]);
        //        var customInstanceType = instance.Data?.CustomInstanceType;
        //        if (string.IsNullOrEmpty(customInstanceType))
        //        {
        //            _logger.LogError($"[{instance.Name}] Plugin job controller instance type is not set, unable to initialize job controller instance");
        //            return;
        //        }
        //        jobController = CreatePluginJobController(customInstanceType, instance, geofences, _serviceProvider);
        //        break;
        //}

        if (jobController == null)
        {
            _logger.LogError($"[{instance.Name}] Unable to instantiate job instance controller with type '{instance.Type}'");
            return;
        }

        _instances.AddOrUpdate(instance.Name, jobController, (key, oldValue) => jobController);

        await Task.CompletedTask;
    }

    public IJobController GetInstanceController(string uuid)
    {
        if (string.IsNullOrEmpty(uuid))
        {
            _logger.LogError($"Failed to get job controller instance for device, UUID was null");
            return null;
        }

        if (!_devices.TryGetValue(uuid, out IDevice? device))
        {
            _logger.LogError($"[{uuid}] Failed to get device from cache, device is not assigned an instance!");
            return null;
        }

        var instanceName = device.InstanceName;
        if (device == null || string.IsNullOrEmpty(instanceName))
        {
            _logger.LogWarning($"Device or device instance name was null, unable to retrieve job controller instance");
            return null;
        }

        return GetInstanceControllerByName(instanceName);
    }

    public IJobController GetInstanceControllerByName(string instanceName)
    {
        if (!_instances.ContainsKey(instanceName))
        {
            _logger.LogError($"[{instanceName}] Unable to get instance controller by name, it does not exist in cache");
            return null;
        }

        if (!_instances.TryGetValue(instanceName, out IJobController? jobController))
        {
            // Failed to get job controller instance
            _logger.LogError($"[{instanceName}] Failed to get job controller instance");
        }
        return jobController;
    }

    public async Task<string> GetStatusAsync(Instance instance)
    {
        if (!_instances.ContainsKey(instance.Name))
        {
            // Instance not started or added to instance cache yet
            return "Starting...";
        }

        if (!_instances.TryGetValue(instance.Name, out IJobController? jobController))
        {
            // Failed to get job controller instance
            _logger.LogError($"[{instance.Name}] Failed to get job controller instance");
        }
        if (jobController != null)
        {
            return await jobController.GetStatusAsync();
        }
        return "Error";
    }

    public void ReloadAllInstances()
    {
        foreach (var (_, instanceController) in _instances)
        {
            instanceController?.ReloadAsync().ConfigureAwait(false);
        }

        _assignmentService.Reload();
    }

    public async Task ReloadInstanceAsync(Instance newInstance, string oldInstanceName)
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
                ((Device)device).InstanceName = newInstance.Name;
                _devices[uuid] = device;
            }
            await _instances[oldInstanceName].StopAsync();
            if (!_instances.TryRemove(oldInstanceName, out _))
            {
                // Failed to remove job controller instance
                _logger.LogError($"[{oldInstanceName}] Failed to remove job controller instance");
            }
            //_instances[oldInstanceName] = null;
        }

        await AddInstanceAsync(newInstance);
    }

    public async Task ReloadInstanceAsync(string instanceName)
    {
        if (!_instances.ContainsKey(instanceName))
        {
            _logger.LogError($"[{instanceName}] Instance does not exist in instance cache, skipping instance reload...");
            return;
        }

        var instance = GetInstanceControllerByName(instanceName);
        if (instance != null)
        {
            await instance.StopAsync();
            await instance.ReloadAsync();
        }
    }

    public async Task RemoveInstanceAsync(string instanceName)
    {
        _instances[instanceName]?.StopAsync();
        //_instances[instanceName] = null;
        if (!_instances.TryRemove(instanceName, out _))
        {
            // Failed to remove instance
            _logger.LogError($"[{instanceName}] Failed to remove instance");
        }

        var devices = _devices.Where(device => string.Compare(device.Value.InstanceName, instanceName, true) == 0);
        foreach (var (uuid, _) in devices)
        {
            //_devices[device.Key] = null;
            if (!_devices.TryRemove(uuid, out _))
            {
                // Failed to remove device
                _logger.LogError($"[{uuid}] Failed to remove device");
            }
        }

        _assignmentService.Reload();
        await Task.CompletedTask;
    }

    #endregion

    #region Queue

    public IReadOnlyList<T> GetQueue<T>(string instanceName)
    {
        var queue = new List<T>();
        var jobController = GetInstanceControllerByName(instanceName);
        if (jobController == null)
            return queue;

        if (jobController is IvInstanceController ivController)
        {
            queue = (List<T>)ivController.GetQueue();
        }
        else if (jobController is AutoInstanceController questController)
        {
            queue = (List<T>)questController.GetQueue();
        }
        return queue;
    }

    public void RemoveFromQueue(string instanceName, string id)
    {
        var jobController = GetInstanceControllerByName(instanceName);
        if (jobController == null)
            return;

        if (jobController is IvInstanceController ivController)
        {
            ivController.RemoveFromQueue(id);
        }
        else if (jobController is AutoInstanceController questController)
        {
            questController.RemoveFromQueue(id);
        }
    }

    public void ClearQueue(string instanceName)
    {
        var jobController = GetInstanceControllerByName(instanceName);
        if (jobController == null)
            return;

        if (jobController is IvInstanceController ivController)
        {
            ivController.ClearQueue();
        }
        else if (jobController is AutoInstanceController questController)
        {
            questController.ClearQueue();
        }
    }

    #endregion

    #region Receivers

    public void GotPokemon(Pokemon pokemon, bool hasIv)
    {
        foreach (var (_, jobController) in _instances)
        {
            if (jobController is IvInstanceController ivController)
            {
                ivController.GotPokemon(pokemon, hasIv);
            }
        }
    }

    public void GotFort(PokemonFortProto fort, string username)
    {
        foreach (var (_, jobController) in _instances)
        {
            if (jobController is LevelingInstanceController levelController)
            {
                levelController.GotFort(fort, username);
            }
        }
    }

    public void GotPlayerInfo(string username, ushort level, ulong xp)
    {
        foreach (var (_, jobController) in _instances)
        {
            if (jobController is LevelingInstanceController levelController)
            {
                levelController.SetPlayerInfo(username, level, xp);
            }
        }
    }

    public TrainerLevelingStatus GetTrainerLevelingStatus(string username)
    {
        var storeLevelingData = true;
        var isTrainerLeveling = false;

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

        return new TrainerLevelingStatus
        {
            Username = username,
            StoreLevelingData = storeLevelingData,
            IsTrainerLeveling = isTrainerLeveling,
        };
    }

    #endregion

    #region Devices

    public void AddDevice(Device device)
    {
        _devices.AddOrUpdate(device.Uuid, device, (key, oldValue) => device);
        //if (!_devices.TryAdd(device.Uuid, device))
        //{
        //    // Failed to add device, might already exist
        //    _logger.LogError($"[{device.Uuid}] Failed to add device, might already exist");
        //}

        _assignmentService.Reload();
    }

    public IEnumerable<string> GetDeviceUuidsInInstance(string instanceName)
    {
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

        _assignmentService.Reload();
        await Task.CompletedTask;
    }

    public void RemoveDevice(string uuid)
    {
        if (!_devices.ContainsKey(uuid))
        {
            _logger.LogError($"[{uuid}] Unable to remove device from cache, it does not exist");
            return;
        }

        if (!_devices.TryRemove(uuid, out _))
        {
            // Failed to remove device
            _logger.LogError($"[{uuid}] Failed to remove device");
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

        var device = (Device)_devices[e.DeviceUuid];
        device.InstanceName = e.InstanceName;

        using var scope = Services.CreateScope();
        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.Devices.UpdateAsync(device);
        await uow.CommitAsync();

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

    private async void OnReloadInstance(object? sender, ReloadInstanceEventArgs e)
    {
        await ReloadInstanceAsync(e.Instance, e.Instance.Name);
    }

    private void OnAccountLevelUp(object? sender, AccountLevelUpEventArgs e)
    {
        var date = e.DateReached
            .FromSeconds()
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

        // Check if time zone service contains our time zone name
        if (!_timeZoneService.TimeZones.ContainsKey(timeZone))
        {
            // If it does not, return UTC+0 offset
            return timeZoneOffset;
        }

        var tzData = _timeZoneService.TimeZones[timeZone];
        timeZoneOffset = enableDst
            // Return DST offset for time zone
            ? tzData.Dst
            // Return non-DST (UTC) offset for time zone
            : tzData.Utc;
        return timeZoneOffset;
    }

    private async Task AssignDevice(Device device, string instanceName)
    {
        device.InstanceName = instanceName;

        using var scope = Services.CreateScope();
        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await uow.Devices.UpdateAsync(device);
        await uow.CommitAsync();
    }

    #endregion

    #region Job Controller Methods

    private static IJobController CreateCircleJobController(Instance instance, CircleInstanceType circleInstanceType, IReadOnlyList<ICoordinate> coords)
    {
        var jobController = new CircleInstanceController(
            instance,
            coords,
            circleInstanceType
        );
        return jobController;
    }

    private static IJobController CreateSmartRaidJobController(IDapperUnitOfWork factory, Instance instance, IReadOnlyList<IMultiPolygon> multiPolygons)
    {
        var jobController = new SmartRaidInstanceController(
            factory,
            instance,
            multiPolygons
        );
        return jobController;
    }

    private static IJobController CreateAutoQuestJobController(IDbContextFactory<MapDbContext> mapFactory, IDbContextFactory<ControllerDbContext> deviceFactory, Instance instance, IReadOnlyList<IMultiPolygon> multiPolygons, short timeZoneOffset)
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

    private static IJobController CreateBootstrapJobController(Instance instance, IReadOnlyList<IMultiPolygon> multiPolygons, IRoutingHost routeGenerator, IRouteCalculator routeCalculator)
    {
        var jobController = new BootstrapInstanceController(
            instance,
            multiPolygons,
            routeGenerator,
            routeCalculator
        );
        return jobController;
    }

    private static IJobController CreateDynamicJobController(Instance instance, IReadOnlyList<IMultiPolygon> multiPolygons, IRoutingHost routeGenerator, IRouteCalculator routeCalculator)
    {
        var jobController = new DynamicRouteInstanceController(
            instance,
            multiPolygons,
            routeGenerator,
            routeCalculator
        );
        return jobController;
    }

    private static IJobController CreateIvJobController(IDapperUnitOfWork factory, Instance instance, IReadOnlyList<IMultiPolygon> multiPolygons, IvList ivList)
    {
        var jobController = new IvInstanceController(
            factory,
            instance,
            multiPolygons,
            ivList.PokemonIds
        );
        return jobController;
    }

    private static IJobController CreateLevelingJobController(IDapperUnitOfWork factory, Instance instance, IReadOnlyList<IMultiPolygon> multiPolygons)
    {
        var jobController = new LevelingInstanceController(
            factory,
            instance,
            multiPolygons
        );
        return jobController;
    }

    private static IJobController CreateSpawnpointJobController(IDapperUnitOfWork uow, Instance instance, IReadOnlyList<IMultiPolygon> multiPolygons, IRouteCalculator routeCalculator)
    {
        var jobController = new TthFinderInstanceController(
            uow,
            instance,
            multiPolygons,
            routeCalculator
        );
        return jobController;
    }

    private static IJobController CreatePluginJobController(string customInstanceType, IInstance instance, IReadOnlyList<Geofence> geofences, ServiceProvider services)
    {
        if (!_pluginInstances.ContainsKey(customInstanceType))
        {
            _logger.LogError($"[{instance.Name}] Plugin job controller has not been registered, unable to initialize job controller instance");
            return null;
        }

        var jobControllerType = _pluginInstances[customInstanceType];
        object? jobControllerInstance = null;
        try
        {
            var args = jobControllerType.BuildJobControllerConstructorArgs(
                instance,
                geofences,
                PluginManager.Instance.Options.SharedServiceHosts,
                services
            );
            if (args?.Any() ?? false)
            {
                jobControllerInstance = Activator.CreateInstance(jobControllerType, args);
            }
            else
            {
                jobControllerInstance = Activator.CreateInstance(jobControllerType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }

        if (jobControllerInstance == null)
        {
            _logger.LogError($"Failed to instantiate a new custom job controller instance for '{instance.Name}'");
            return null;
        }

        var jobController = (IJobController)jobControllerInstance;
        return jobController;
    }

    #endregion
}