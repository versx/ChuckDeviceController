namespace ChuckDeviceController.JobControllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Geofence.Models;
    using ChuckDeviceController.JobControllers.Instances;

    public class InstanceController
    {
        #region Variables

        private readonly ILogger<InstanceController> _logger;

        private readonly IDictionary<string, Device> _devices;
        private readonly IDictionary<string, IJobController> _instances;
        private readonly DeviceRepository _deviceRepository;
        private readonly InstanceRepository _instanceRepository;

        private readonly object _instancesLock = new object();
        private readonly object _devicesLock = new object();

        #endregion

        #region Singleton

        private static InstanceController _instance;
        public static InstanceController Instance
        {
            get
            {
                return _instance ??= new InstanceController();
            }
        }

        #endregion

        #region Constructor

        public InstanceController()
        {
            _devices = new Dictionary<string, Device>();
            _instances = new Dictionary<string, IJobController>();
            _deviceRepository = new DeviceRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));
            _instanceRepository = new InstanceRepository(DbContextFactory.CreateDeviceControllerContext(Startup.DbConfig.ToString()));

            _logger = new Logger<InstanceController>(LoggerFactory.Create(x => x.AddConsole()));
            _logger.LogInformation("Starting instances...");

            var instances = _instanceRepository.GetAllAsync()
                                               .ConfigureAwait(false)
                                               .GetAwaiter()
                                               .GetResult();
            var devices = _deviceRepository.GetAllAsync()
                                           .ConfigureAwait(false)
                                           .GetAwaiter()
                                           .GetResult();
            foreach (var instance in instances)
            {
                if (!ThreadPool.QueueUserWorkItem(_ =>
                {
                    _logger.LogInformation($"Starting {instance.Name}");
                    AddInstance(instance);
                    _logger.LogInformation($"Started {instance.Name}");
                    foreach (var device in devices.AsEnumerable().Where(d => string.Compare(d.InstanceName, instance.Name, true) == 0))
                    {
                        AddDevice(device);
                    }
                }))
                {
                    _logger.LogError($"Failed to start instance {instance.Name}");
                }
            }
            _logger.LogInformation("Done starting instances");
        }

        #endregion

        #region Public Methods

        #region Instances

        public IJobController GetInstanceController(string uuid)
        {
            lock (_devicesLock)
            {
                if (!_devices.ContainsKey(uuid))
                {
                    _logger.LogWarning($"[{uuid}] Not assigned an instance!");
                    return null;
                }

                var device = _devices[uuid];
                var instanceName = device.InstanceName;
                if (device == null && string.IsNullOrEmpty(instanceName))
                    return null;

                return GetInstanceControllerByName(instanceName);
            }
        }

        public async Task<string> GetInstanceStatus(Instance instance)
        {
            // TODO: Lock
            if (!_instances.ContainsKey(instance.Name))
            {
                // Instance not started yet
                return "Starting...";
            }
            try
            {
                var instanceController = _instances[instance.Name];
                // TODO: Maybe no locking object
                return await (instanceController?.GetStatus()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get instance status for instance {instance.Name}: {ex}");
            }
            return "Error";
        }

        public void AddInstance(Instance instance)
        {
            IJobController instanceController = null;
            switch (instance.Type)
            {
                case InstanceType.CirclePokemon:
                case InstanceType.CircleRaid:
                case InstanceType.SmartCircleRaid:
                    try
                    {
                        var coordsArray = (List<Coordinate>)
                        (
                            instance.Data.Area is List<Coordinate>
                                ? instance.Data.Area
                                : JsonSerializer.Deserialize<List<Coordinate>>(Convert.ToString(instance.Data.Area))
                        );
                        var minLevel = instance.Data.MinimumLevel;
                        var maxLevel = instance.Data.MaximumLevel;
                        switch (instance.Type)
                        {
                            case InstanceType.CirclePokemon:
                                instanceController = new CircleInstanceController(instance.Name, coordsArray, CircleType.Pokemon, CircleRouteType.Split, minLevel, maxLevel);
                                break;
                            case InstanceType.CircleRaid:
                                instanceController = new CircleInstanceController(instance.Name, coordsArray, CircleType.Raid, CircleRouteType.Split, minLevel, maxLevel);
                                break;
                            case InstanceType.SmartCircleRaid:
                                // TODO: SmartCircleRaidInstanceController
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex}");
                    }
                    break;
                case InstanceType.AutoQuest:
                case InstanceType.PokemonIV:
                case InstanceType.Bootstrap:
                    try
                    {
                        var coordsArray = (List<List<Coordinate>>)
                        (
                            instance.Data.Area is List<List<Coordinate>>
                                ? instance.Data.Area
                                : JsonSerializer.Deserialize<List<List<Coordinate>>>(Convert.ToString(instance.Data.Area))
                        );
                        var areaArrayEmptyInner = new List<MultiPolygon>();
                        foreach (var coords in coordsArray)
                        {
                            var multiPolygon = new MultiPolygon();
                            foreach (var coord in coords)
                            {
                                multiPolygon.Add(new Polygon { coord.Latitude, coord.Longitude });
                            }
                            areaArrayEmptyInner.Add(multiPolygon);
                        }
                        var minLevel = instance.Data.MinimumLevel;
                        var maxLevel = instance.Data.MaximumLevel;
                        switch (instance.Type)
                        {
                            case InstanceType.AutoQuest:
                                var timezoneOffset = instance.Data.TimezoneOffset ?? 0;
                                var spinLimit = instance.Data.SpinLimit ?? 3500;
                                instanceController = new AutoInstanceController(instance.Name, areaArrayEmptyInner, AutoType.Quest, timezoneOffset, minLevel, maxLevel, spinLimit);
                                break;
                            case InstanceType.PokemonIV:
                                var pokemonList = instance.Data.PokemonIds;
                                var ivQueueLimit = instance.Data.IVQueueLimit ?? 100;
                                instanceController = new IVInstanceController(instance.Name, areaArrayEmptyInner, pokemonList, minLevel, maxLevel, ivQueueLimit);
                                break;
                            case InstanceType.Bootstrap:
                                // TODO: Configurable circle size
                                instanceController = new BootstrapInstanceController(instance.Name, coordsArray, minLevel, maxLevel, 70);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error: {ex}");
                    }
                    break;
            }
            lock (_instancesLock)
            {
                _instances[instance.Name] = instanceController;
            }
        }

        public void ReloadInstance(Instance newInstance, string oldInstanceName)
        {
            lock (_instancesLock)
            {
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
                    _instances[oldInstanceName].Stop();
                    _instances[oldInstanceName] = null;
                }
            }
            AddInstance(newInstance);
        }

        public void ReloadAll()
        {
            lock (_instancesLock)
            {
                foreach (var (_, instanceController) in _instances)
                {
                    instanceController.Reload();
                }
            }
        }

        public async Task RemoveInstance(string instanceName)
        {
            lock (_instancesLock)
            {
                _instances[instanceName].Stop();
                _instances[instanceName] = null;
                var devices = _devices.Where(d => string.Compare(d.Value.InstanceName, instanceName, true) == 0);
                foreach (var device in devices)
                {
                    _devices[device.Key] = null;
                }
            }
            await AssignmentController.Instance.Initialize().ConfigureAwait(false);
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
        }

        public async Task RemoveDevice(Device device)
        {
            RemoveDevice(device.Uuid);
            await AssignmentController.Instance.Initialize().ConfigureAwait(false);
        }

        public void RemoveDevice(string uuid)
        {
            lock (_devicesLock)
            {
                if (_devices.ContainsKey(uuid))
                {
                    _devices.Remove(uuid);
                }
            }
        }

        public void ReloadDevice(Device newDevice, string oldDeviceUuid)
        {
            RemoveDevice(oldDeviceUuid);
            AddDevice(newDevice);
        }

        public List<string> GetDeviceUuidsInInstance(string instanceName)
        {
            lock (_devicesLock)
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
        }

        #endregion

        public List<Pokemon> GetIVQueue(string name)
        {
            lock (_instancesLock)
            {
                if (_instances.ContainsKey(name))
                {
                    var instance = _instances[name];
                    if (instance is IVInstanceController iv)
                        return iv.GetQueue();
                }
                return new List<Pokemon>();
            }
        }

        public void GotPokemon(Pokemon pokemon)
        {
            lock (_instancesLock)
            {
                foreach (var (_, instanceController) in _instances)
                {
                    if (instanceController is IVInstanceController iv)
                    {
                        iv.AddPokemon(pokemon);
                    }
                }
            }
        }

        public void GotIV(Pokemon pokemon)
        {
            lock (_instancesLock)
            {
                foreach (var (_, instanceController) in _instances)
                {
                    if (instanceController is IVInstanceController iv)
                    {
                        iv.GotIV(pokemon);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private IJobController GetInstanceControllerByName(string name)
        {
            lock (_instancesLock)
            {
                if (!_instances.ContainsKey(name))
                    return null;

                return _instances[name];
            }
        }

        private async Task RemoveInstance(Instance instance)
        {
            await RemoveInstance(instance.Name).ConfigureAwait(false);
        }

        #endregion
    }
}