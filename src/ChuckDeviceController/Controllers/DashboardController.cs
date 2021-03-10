namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;
    using Z.EntityFramework.Plus;

    using Chuck.Common;
    using Chuck.Common.Utilities;
    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using ChuckDeviceController.Converters;
    using ChuckDeviceController.JobControllers;
    using ChuckDeviceController.Services;

    [Controller]
    public class DashboardController : Controller
    {
        #region Variables

        // Dependency injection variables
        private readonly DeviceControllerContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabaseAsync _redisDatabase;
        private readonly ISubscriber _subscriber;
        private readonly ILogger<DeviceController> _logger;

        private readonly AccountRepository _accountRepository;
        private readonly AssignmentRepository _assignmentRepository;
        private readonly DeviceRepository _deviceRepository;
        private readonly InstanceRepository _instanceRepository;
        private readonly PokemonRepository _pokemonRepository;
        private readonly PokestopRepository _pokestopRepository;
        private readonly GeofenceRepository _geofenceRepository;
        private readonly WebhookRepository _webhookRepository;

        private readonly DeviceGroupRepository _deviceGroupRepository;

        #endregion

        #region Constructor

        public DashboardController(DeviceControllerContext context, IConnectionMultiplexer connectionMultiplexer, ILogger<DeviceController> logger)
        {
            _context = context;
            _redis = connectionMultiplexer;
            _redisDatabase = _redis.GetDatabase(Startup.Config.Redis.DatabaseNum);
            _subscriber = _redis.GetSubscriber();
            _logger = logger;

            _accountRepository = new AccountRepository(_context);
            _assignmentRepository = new AssignmentRepository(_context);
            _deviceRepository = new DeviceRepository(_context);
            _instanceRepository = new InstanceRepository(_context);
            _pokestopRepository = new PokestopRepository(_context);
            _pokemonRepository = new PokemonRepository(_context);
            _geofenceRepository = new GeofenceRepository(_context);
            _webhookRepository = new WebhookRepository(_context);
            _deviceGroupRepository = new DeviceGroupRepository(_context);
        }

        #endregion

        #region Routes

        [HttpGet("/")]
        public IActionResult GetIndex() => Redirect("/dashboard");

        [HttpGet("/dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            dynamic obj = BuildDefaultData();
            obj.devices_count = (await _context.Devices.AsNoTracking().DeferredCount().FromCacheAsync().ConfigureAwait(false)).ToString("N0");
            obj.instances_count = (await _context.Instances.AsNoTracking().DeferredCount().FromCacheAsync().ConfigureAwait(false)).ToString("N0");
            obj.assignments_count = (await _context.Assignments.AsNoTracking().DeferredCount().FromCacheAsync().ConfigureAwait(false)).ToString("N0");
            obj.accounts_count = (await _context.Accounts.AsNoTracking().DeferredCount().FromCacheAsync().ConfigureAwait(false)).ToString("N0");
            obj.geofences_count = (await _context.Geofences.AsNoTracking().DeferredCount().FromCacheAsync().ConfigureAwait(false)).ToString("N0");
            obj.webhooks_count = (await _context.Webhooks.AsNoTracking().DeferredCount().FromCacheAsync().ConfigureAwait(false)).ToString("N0");
            obj.devicegroups_count = (await _context.DeviceGroups.AsNoTracking().DeferredCount().FromCacheAsync().ConfigureAwait(false)).ToString("N0");
            var data = TemplateRenderer.ParseTemplate("index", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        #region Devices

        [HttpGet("/dashboard/devices")]
        public IActionResult GetDevices()
        {
            var obj = BuildDefaultData();
            var data = TemplateRenderer.ParseTemplate("devices", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        [
            HttpGet("/dashboard/device/assign/{uuid}"),
            HttpPost("/dashboard/device/assign/{uuid}"),
        ]
        public async Task<IActionResult> AssignDevice(string uuid)
        {
            var device = await _deviceRepository.GetByIdAsync(uuid).ConfigureAwait(false);
            if (device == null)
            {
                // Unknown device
                return BuildErrorResponse("device-assign", $"Failed to retrieve device '{uuid}'");
            }
            if (Request.Method == "GET")
            {
                var instances = (await _instanceRepository.GetAllAsync().ConfigureAwait(false)).Select(x => new
                {
                    name = x.Name,
                    selected = string.Compare(x.Name, device.InstanceName, true) == 0
                }).ToList();
                dynamic obj = BuildDefaultData();
                obj.device_uuid = uuid;
                obj.instances = instances;
                var data = TemplateRenderer.ParseTemplate("device-assign", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var instanceName = Request.Form["instance"];
                if (string.IsNullOrEmpty(instanceName))
                {
                    // Unknown instance name provided
                    return BuildErrorResponse("device-assign", $"Instance '{instanceName}' does not exist");
                }
                var instance = await _instanceRepository.GetByIdAsync(instanceName).ConfigureAwait(false);
                if (instance == null)
                {
                    // Failed to get instance by name
                    return BuildErrorResponse("device-assign", $"Failed to retrieve instance '{instanceName}'");
                }
                device.InstanceName = instance.Name;
                await _deviceRepository.UpdateAsync(device).ConfigureAwait(false);
                //templateData = Renderer.ParseTemplate("devices", BuildDefaultData());
                InstanceController.Instance.ReloadDevice(device, uuid);
                return Redirect("/dashboard/devices");
            }
        }

        #endregion

        #region Device Groups

        [
            HttpGet("/dashboard/devicegroups"),
            HttpPost("/dashboard/devicegroups"),
        ]
        public IActionResult GetDeviceGroups()
        {
            var obj = BuildDefaultData();
            var data = TemplateRenderer.ParseTemplate("devicegroups", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        [
            HttpGet("/dashboard/devicegroup/add"),
            HttpPost("/dashboard/devicegroup/add"),
        ]
        public async Task<IActionResult> AddDeviceGroup()
        {
            if (Request.Method == "GET")
            {
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                dynamic obj = BuildDefaultData();
                obj.devices = devices.Select(x => new
                {
                    name = x.Uuid,
                    selected = false,
                });
                obj.nothing_selected = true;
                var data = TemplateRenderer.ParseTemplate("devicegroup-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var name = Request.Form["name"].ToString();
                var devices = Request.Form["devices"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)?.ToList();
                var deviceGroup = await _deviceGroupRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (deviceGroup != null)
                {
                    return BuildErrorResponse("devicegroup-add", $"Device Group with name '{name}' already exists");
                }
                await _deviceGroupRepository.AddOrUpdateAsync(new DeviceGroup
                {
                    Name = name,
                    Devices = devices
                });
                return Redirect("/dashboard/devicegroups");
            }
        }

        [
            HttpGet("/dashboard/devicegroup/edit/{name}"),
            HttpPost("/dashboard/devicegroup/edit/{name}"),
        ]
        public async Task<IActionResult> EditDeviceGroup(string name)
        {
            if (Request.Method == "GET")
            {
                var deviceGroup = await _deviceGroupRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (deviceGroup == null)
                {
                    return BuildErrorResponse("devicegroup-edit", $"Device Group with name '{name}' does not exist");
                }
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                dynamic obj = BuildDefaultData();
                obj.name = deviceGroup.Name;
                obj.old_name = deviceGroup.Name;
                obj.devices = devices.Select(x => new
                {
                    name = x.Uuid,
                    selected = deviceGroup.Devices.Contains(x.Uuid),
                });
                obj.nothing_selected = true;
                var data = TemplateRenderer.ParseTemplate("devicegroup-edit", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                // TODO: Check if new name does not exist and old name does
                var newName = Request.Form["name"].ToString();
                var oldName = Request.Form["old_name"].ToString();
                var devices = Request.Form["devices"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)?.ToList();
                var deviceGroup = await _deviceGroupRepository.GetByIdAsync(oldName).ConfigureAwait(false);
                if (deviceGroup == null)
                {
                    return BuildErrorResponse("devicegroup-add", $"Device Group with name '{oldName}' does not exist");
                }

                // TODO: Add support for renaming (need to delete old and create new)
                deviceGroup.Name = oldName;
                deviceGroup.Devices = devices;
                await _deviceGroupRepository.SaveAsync().ConfigureAwait(false);
                return Redirect("/dashboard/devicegroups");
            }
        }

        [
            HttpGet("/dashboard/devicegroup/assign/{name}"),
            HttpPost("/dashboard/devicegroup/assign/{name}"),
        ]
        public async Task<IActionResult> AssignDeviceGroup(string name)
        {
            if (Request.Method == "GET")
            {
                var deviceGroup = await _deviceGroupRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (deviceGroup == null)
                {
                    return BuildErrorResponse("devicegroup-assign", $"Device Group with name '{name}' does not exist");
                }
                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                dynamic obj = BuildDefaultData();
                obj.name = deviceGroup.Name;
                obj.instances = instances.Select(x => new
                {
                    name = x.Name,
                    selected = false,
                });
                obj.nothing_selected = true;
                var data = TemplateRenderer.ParseTemplate("devicegroup-assign", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var deviceGroup = await _deviceGroupRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (deviceGroup == null)
                {
                    return BuildErrorResponse("devicegroup-assign", $"Device Group with name '{name}' does not exist");
                }

                var instanceName = Request.Form["instance"].ToString();
                var instance = await _instanceRepository.GetByIdAsync(instanceName).ConfigureAwait(false);
                if (instance == null)
                {
                    return BuildErrorResponse("devicegroup-instance", $"Instance with name '{name}' does not exist");
                }

                var devices = await _deviceRepository.GetByIdsAsync(deviceGroup.Devices);
                foreach (var device in devices)
                {
                    device.InstanceName = instance.Name;
                }
                await _deviceRepository.AddOrUpdateAsync(devices).ConfigureAwait(false);
                // :see_no_evil: TODO: Fix eventually instead of looping twice
                foreach (var device in devices)
                {
                    InstanceController.Instance.ReloadDevice(device, device.Uuid);
                }
                return Redirect("/dashboard/devicegroups");
            }
        }

        [HttpGet("/dashboard/devicegroup/delete/{name}")]
        public async Task<IActionResult> DeleteDeviceGroup(string name)
        {
            var deviceGroup = await _deviceGroupRepository.GetByIdAsync(name).ConfigureAwait(false);
            if (deviceGroup == null)
            {
                // Failed to get device group, does it exist?
                return BuildErrorResponse("devicegroup-delete", $"Device Group with name '{name}' does not exist");
            }
            await _deviceGroupRepository.DeleteAsync(deviceGroup).ConfigureAwait(false);
            return Redirect("/dashboard/devicegroups");
        }

        #endregion

        #region Instances

        [HttpGet("/dashboard/instances")]
        public IActionResult GetInstances()
        {
            var obj = BuildDefaultData();
            var data = TemplateRenderer.ParseTemplate("instances", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        [
            HttpGet("/dashboard/instance/add"),
            HttpPost("/dashboard/instance/add"),
        ]
        public async Task<IActionResult> AddInstance()
        {
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                obj.timezone_offset = 0;
                obj.min_level = 0;
                obj.max_level = 30;
                var geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
                obj.geofences = geofences.Select(x => new
                {
                    name = x.Name,
                    type = x.Type.ToString().ToLower(),
                    selected = false,
                });
                obj.timezone_offset = 0;
                var timezones = TimeZoneService.Instance.Timezones.Keys.ToList();
                timezones.Sort();
                obj.timezones = timezones.Select(x => new
                {
                    name = x,
                    selected = false,
                });
                obj.circle_size = 70;
                obj.nothing_selected = true;
                obj.iv_queue_limit = 100;
                obj.spin_limit = 3500;
                obj.quest_retry_limit = 5;
                obj.account_group = null;
                obj.is_event = false;
                obj.enable_dst = false;
                var data = TemplateRenderer.ParseTemplate("instance-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var name = Request.Form["name"].ToString();
                var type = Instance.StringToInstanceType(Request.Form["type"]);
                var geofence = Request.Form["geofence"].ToString();
                var area = Request.Form["area"].ToString();
                var minLevel = ushort.Parse(Request.Form["min_level"]);
                var maxLevel = ushort.Parse(Request.Form["max_level"]);
                var timezone = Request.Form["timezone"].ToString();
                var circleRouteType = Request.Form.ContainsKey("circle_route_type")
                    ? StringToCircleRouteType(Request.Form["circle_route_type"])
                    : CircleRouteType.Default;
                var circleSize = Request.Form.ContainsKey("circle_size")
                    ? ushort.Parse(Request.Form["circle_size"].ToString() ?? "70")
                    : 70;
                var pokemonIdsValue = Request.Form["pokemon_ids"].ToString();
                var pokemonIds = pokemonIdsValue == "*"
                    ? Enumerable.Range(1, 999).Select(x => (uint)x).ToList()
                    : pokemonIdsValue?.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)?.Select(uint.Parse).ToList();
                var fastBootstrapMode = Request.Form["fast_bootstrap_mode"].ToString() == "on";
                var accountGroup = Request.Form["account_group"].ToString();
                var isEvent = Request.Form["is_event"].ToString() == "on";
                var enableDst = Request.Form["enable_dst"].ToString() == "on";
                ushort ivQueueLimit = 100;
                ushort spinLimit = 3500;
                ushort questRetryLimit = 5;
                if (type == InstanceType.PokemonIV)
                {
                    if (Request.Form.ContainsKey("iv_queue_limit"))
                    {
                        ivQueueLimit = ushort.Parse(Request.Form["iv_queue_limit"]);
                    }
                }
                else if (type == InstanceType.AutoQuest)
                {
                    if (Request.Form.ContainsKey("spin_limit"))
                    {
                        spinLimit = ushort.Parse(Request.Form["spin_limit"]);
                    }
                    if (Request.Form.ContainsKey("quest_retry_limit"))
                    {
                        questRetryLimit = byte.Parse(Request.Form["quest_retry_limit"].ToString());
                        if (questRetryLimit > byte.MaxValue || questRetryLimit <= byte.MinValue)
                        {
                            return BuildErrorResponse("instance-add", "Invalid Quest Retry Limit value (Valid value: 1-255)");
                        }
                    }
                }

                if (minLevel > maxLevel || minLevel == 0 || minLevel > 40 || maxLevel == 0 || maxLevel > 40)
                {
                    // Invalid levels
                    return BuildErrorResponse("instance-add", "Invalid minimum and maximum levels provided");
                }

                var instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (instance != null)
                {
                    // Instance already exists
                    return BuildErrorResponse("instance-add", $"Instance with name '{name}' already exists");
                }

                instance = new Instance
                {
                    Name = name,
                    Type = type,
                    Geofence = geofence,
                    Data = new InstanceData
                    {
                        IVQueueLimit = ivQueueLimit,
                        SpinLimit = spinLimit,
                        MinimumLevel = minLevel,
                        MaximumLevel = maxLevel,
                        PokemonIds = pokemonIds,
                        Timezone = timezone,
                        EnableDst = enableDst,
                        CircleRouteType = circleRouteType,
                        CircleSize = (ushort)circleSize,
                        FastBootstrapMode = fastBootstrapMode,
                        AccountGroup = accountGroup,
                        IsEvent = isEvent,
                    }
                };
                await _instanceRepository.AddAsync(instance).ConfigureAwait(false);
                await InstanceController.Instance.AddInstance(instance).ConfigureAwait(false);
                return Redirect("/dashboard/instances");
            }
        }

        [
            HttpGet("/dashboard/instance/edit/{name}"),
            HttpPost("/dashboard/instance/edit/{name}"),
        ]
        public async Task<IActionResult> EditInstance(string name)
        {
            if (Request.Method == "GET")
            {
                var instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (instance == null)
                {
                    // Failed to get instance by name
                    return BuildErrorResponse("instance-edit", $"Instance with name '{name}' does not exist");
                }
                var minLevel = instance.Data.MinimumLevel;
                var maxLevel = instance.Data.MaximumLevel;
                dynamic obj = BuildDefaultData();
                obj.name = name;
                obj.geofence = instance.Geofence;
                obj.old_name = name;
                obj.min_level = minLevel;
                obj.max_level = maxLevel;
                obj.account_group = instance.Data.AccountGroup;
                obj.is_event = instance.Data.IsEvent ? "checked" : null;
                obj.circle_pokemon_selected = instance.Type == InstanceType.CirclePokemon;
                obj.circle_raid_selected = instance.Type == InstanceType.CircleRaid;
                obj.pokemon_iv_selected = instance.Type == InstanceType.PokemonIV;
                obj.auto_quest_selected = instance.Type == InstanceType.AutoQuest;
                obj.bootstrap_selected = instance.Type == InstanceType.Bootstrap;
                obj.find_tth_selected = instance.Type == InstanceType.FindTTH;
                var timezones = TimeZoneService.Instance.Timezones.Keys.ToList();
                timezones.Sort();
                obj.timezones = timezones.Select(x => new
                {
                    name = x,
                    selected = x == instance.Data.Timezone,
                });
                obj.enable_dst = instance.Data.EnableDst ? "checked" : null;
                var geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
                obj.geofences = geofences.Select(x => new
                {
                    name = x.Name,
                    type = x.Type.ToString().ToLower(),
                    selected = string.Compare(instance.Geofence, x.Name, true) == 0,
                });
                var geofence = geofences.FirstOrDefault(x => string.Compare(x.Name, instance.Geofence, true) == 0);
                obj.circle_route_type = CircleRouteTypeToString(instance.Data.CircleRouteType);
                obj.leapfrog_selected = instance.Data.CircleRouteType == CircleRouteType.Default;
                obj.spread_selected = instance.Data.CircleRouteType == CircleRouteType.Split;
                obj.circular_selected = instance.Data.CircleRouteType == CircleRouteType.Circular;
                //switch (instance.Type)
                //{
                //    case InstanceType.CirclePokemon:
                obj.circle_route_type = instance.Data.CircleRouteType;
                //        break;
                //    case InstanceType.PokemonIV:
                obj.pokemon_ids = instance.Data.PokemonIds == null ? null : string.Join("\n", instance.Data.PokemonIds);
                obj.iv_queue_limit = instance.Data.IVQueueLimit > 0 ? instance.Data.IVQueueLimit : 100;
                //        break;
                //    case InstanceType.AutoQuest:
                obj.spin_limit = instance.Data.SpinLimit > 0 ? instance.Data.SpinLimit : 3500;
                obj.quest_retry_limit = instance.Data.QuestRetryLimit > 0 ? instance.Data.QuestRetryLimit : 5;
                //        break;
                //    case InstanceType.Bootstrap:
                obj.circle_size = instance.Data.CircleSize ?? 70;
                obj.fast_bootstrap_mode = instance.Data.FastBootstrapMode;
                //        break;
                //}
                var data = TemplateRenderer.ParseTemplate("instance-edit", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                if (Request.Form.ContainsKey("delete"))
                {
                    var instanceToDelete = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                    if (instanceToDelete != null)
                    {
                        await _instanceRepository.DeleteAsync(instanceToDelete).ConfigureAwait(false);
                        await InstanceController.Instance.RemoveInstance(name).ConfigureAwait(false);
                        _logger.LogDebug($"Instance {name} was deleted");
                    }
                    return Redirect("/dashboard/instances");
                }
                else if (Request.Form.ContainsKey("clear_quests"))
                {
                    // Clear quests for instance
                    var instanceToDelete = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                    if (instanceToDelete != null)
                    {
                        await _pokestopRepository.ClearQuestsAsync(name).ConfigureAwait(false);
                        _logger.LogDebug($"Quests were cleared for instance {name}");
                    }
                    return Redirect("/dashboard/instances");
                }

                var newName = Request.Form["name"].ToString();
                var type = Instance.StringToInstanceType(Request.Form["type"]);
                var geofence = Request.Form["geofence"].ToString();
                //var area = Request.Form["area"].ToString();
                var minLevel = ushort.Parse(Request.Form["min_level"]);
                var maxLevel = ushort.Parse(Request.Form["max_level"]);
                var timezone = Request.Form["timezone"].ToString();
                var circleSize = ushort.Parse(Request.Form["circle_Size"].ToString() ?? "70");
                var circleRouteType = Request.Form.ContainsKey("circle_route_type")
                    ? StringToCircleRouteType(Request.Form["circle_route_type"].ToString())
                    : CircleRouteType.Default;
                var pokemonIdsValue = Request.Form["pokemon_ids"].ToString();
                var pokemonIds = pokemonIdsValue == "*"
                    ? Enumerable.Range(1, 999).Select(x => (uint)x).ToList()
                    : pokemonIdsValue?.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)?.Select(uint.Parse).ToList();
                var ivQueueLimit = ushort.Parse(Request.Form["iv_queue_limit"]);
                var spinLimit = ushort.Parse(Request.Form["spin_limit"]);
                var fastBootstrapMode = Request.Form["fast_bootstrap_mode"].ToString() == "on";
                var questRetryLimit = byte.Parse(Request.Form["quest_retry_limit"].ToString());
                var accountGroup = Request.Form["account_group"].ToString();
                var isEvent = Request.Form["is_event"].ToString() == "on";
                var enableDst = Request.Form["enable_dst"].ToString() == "on";
                if (minLevel > maxLevel || minLevel == 0 || minLevel > 40 || maxLevel == 0 || maxLevel > 40)
                {
                    // Invalid levels
                    return BuildErrorResponse("instance-edit", "Invalid minimum and maximum levels provided");
                }
                if (questRetryLimit > byte.MaxValue || questRetryLimit <= byte.MinValue)
                {
                    return BuildErrorResponse("instance-edit", "Invalid Quest Retry Limit value (Valid value: 1-255)");
                }

                var instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (instance == null)
                {
                    // Instance does not exist, create?
                    return BuildErrorResponse("instance-edit", $"Instance with name '{name}' does not exist");
                }

                instance.Name = newName;
                instance.Type = type;
                instance.Geofence = geofence;
                instance.Data = new InstanceData
                {
                    IVQueueLimit = ivQueueLimit,
                    CircleRouteType = circleRouteType,
                    CircleSize = circleSize,
                    SpinLimit = spinLimit,
                    MinimumLevel = minLevel,
                    MaximumLevel = maxLevel,
                    PokemonIds = pokemonIds,
                    Timezone = timezone,
                    EnableDst = enableDst,
                    FastBootstrapMode = fastBootstrapMode,
                    AccountGroup = accountGroup,
                    IsEvent = isEvent,
                };
                await _instanceRepository.UpdateAsync(instance).ConfigureAwait(false);
                await InstanceController.Instance.ReloadInstance(instance, name).ConfigureAwait(false);
                _logger.LogDebug($"Instance {name} was updated");
                return Redirect("/dashboard/instances");
            }
        }

        [HttpGet("/dashboard/instance/ivqueue/{name}")]
        public IActionResult GetIVQueue(string name)
        {
            dynamic obj = BuildDefaultData();
            obj.instance_name = name;
            var data = TemplateRenderer.ParseTemplate("instance-ivqueue", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        #endregion

        #region Geofences

        [HttpGet("/dashboard/geofences")]
        public IActionResult GetGeofences()
        {
            var obj = BuildDefaultData();
            var data = TemplateRenderer.ParseTemplate("geofences", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        [
            HttpGet("/dashboard/geofence/add"),
            HttpPost("/dashboard/geofence/add"),
        ]
        public async Task<IActionResult> AddGeofence()
        {
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                var data = TemplateRenderer.ParseTemplate("geofence-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var name = Request.Form["name"].ToString();
                var type = Request.Form["type"].ToString() == "circle"
                    ? GeofenceType.Circle
                    : GeofenceType.Geofence;
                var area = Request.Form["area"].ToString();

                var geofence = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (geofence != null)
                {
                    // Geofence already exists by name
                    return BuildErrorResponse("geofence-add", $"Geofence with name '{name}' already exists");
                }

                dynamic newArea = null;
                switch (type)
                {
                    case GeofenceType.Circle:
                        {
                            // Parse area
                            var coords = AreaConverters.AreaStringToCoordinates(area);
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                    case GeofenceType.Geofence:
                        {
                            var coords = AreaConverters.AreaStringToMultiPolygon(area);
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                }

                geofence = new Geofence
                {
                    Name = name,
                    Type = type,
                    Data = new GeofenceData
                    {
                        Area = newArea,
                    }
                };
                await _geofenceRepository.AddAsync(geofence).ConfigureAwait(false);
                InstanceController.Instance.ReloadAll();
                return Redirect("/dashboard/geofences");
            }
        }

        [
            HttpGet("/dashboard/geofence/edit/{name}"),
            HttpPost("/dashboard/geofence/edit/{name}"),
        ]
        public async Task<IActionResult> EditGeofence(string name)
        {
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                var geofence = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (geofence == null)
                {
                    return Redirect("/dashboard/geofences");
                }
                obj.name = geofence.Name;
                obj.old_name = geofence.Name;
                obj.circle_selected = geofence.Type == GeofenceType.Circle;
                obj.geofence_selected = geofence.Type == GeofenceType.Geofence;
                var coords = string.Empty;
                var coordsArray = geofence?.Data?.Area;
                if (geofence.Type == GeofenceType.Circle)
                {
                    coords = AreaConverters.CoordinatesToAreaString(coordsArray);
                }
                else if (geofence.Type == GeofenceType.Geofence)
                {
                    coords = AreaConverters.MultiPolygonToAreaString(coordsArray);
                }
                obj.area = coords;
                var data = TemplateRenderer.ParseTemplate("geofence-edit", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                if (Request.Form.ContainsKey("delete"))
                {
                    var geofenceToDelete = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
                    if (geofenceToDelete != null)
                    {
                        await _geofenceRepository.DeleteAsync(geofenceToDelete).ConfigureAwait(false);
                        _logger.LogDebug($"Geofence {name} was deleted");
                    }
                    return Redirect("/dashboard/geofences");
                }

                var newName = Request.Form["name"].ToString();
                var type = Request.Form["type"].ToString() == "circle"
                    ? GeofenceType.Circle
                    : GeofenceType.Geofence;
                var area = Request.Form["area"].ToString();

                var geofence = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (geofence == null)
                {
                    // Failed to find geofence by by name
                    return BuildErrorResponse("geofence-edit", $"Geofence with name '{name}' does not exist");
                }

                dynamic newArea = null;
                switch (type)
                {
                    case GeofenceType.Circle:
                        {
                            // Parse area
                            var coords = AreaConverters.AreaStringToCoordinates(area);
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                    case GeofenceType.Geofence:
                        {
                            var coords = AreaConverters.AreaStringToMultiPolygon(area);
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                }
                geofence.Name = newName;
                geofence.Type = type;
                geofence.Data = new GeofenceData
                {
                    Area = newArea,
                };
                await _geofenceRepository.UpdateAsync(geofence).ConfigureAwait(false);
                InstanceController.Instance.ReloadAll();
                return Redirect("/dashboard/geofences");
            }
        }

        #endregion

        #region Assignments

        [HttpGet("/dashboard/assignments")]
        public IActionResult GetAssignments()
        {
            var obj = BuildDefaultData();
            var data = TemplateRenderer.ParseTemplate("assignments", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        [
            HttpGet("/dashboard/assignment/add"),
            HttpPost("/dashboard/assignment/add"),
        ]
        public async Task<IActionResult> AddAssignment()
        {
            if (Request.Method == "GET")
            {
                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                var deviceGroups = await _deviceGroupRepository.GetAllAsync().ConfigureAwait(false);
                dynamic obj = BuildDefaultData();
                obj.instances = instances.Select(x => new { name = x.Name, selected = false, selected_source = false });
                obj.devices = devices.Select(x => new { uuid = x.Uuid, selected = false });
                obj.device_groups = deviceGroups.Select(x => new { uuid = x.Name, selected = false });
                obj.nothing_selected = true;
                obj.nothing_selected_source = true;
                var data = TemplateRenderer.ParseTemplate("assignment-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var deviceOrDeviceGroupName = Request.Form["device"].ToString();
                var uuid = deviceOrDeviceGroupName.StartsWith("device:") ? new string(deviceOrDeviceGroupName.Skip(7).ToArray()) : null;
                var deviceGroupName = deviceOrDeviceGroupName.StartsWith("group:") ? new string(deviceOrDeviceGroupName.Skip(6).ToArray()) : null;
                var sourceInstance = Request.Form["source_instance"].ToString();
                sourceInstance = string.IsNullOrEmpty(sourceInstance) ? null : sourceInstance;
                var destinationInstance = Request.Form["instance"].ToString();
                var time = Request.Form["time"].ToString();
                var date = Request.Form["date"].ToString();
                var createOnComplete = Request.Form["oncomplete"].ToString() == "on";
                var enabled = Request.Form["enabled"].ToString() == "on";

                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                var deviceGroups = await _deviceGroupRepository.GetAllAsync().ConfigureAwait(false);
                if (instances == null || devices == null || deviceGroups == null)
                {
                    // Failed to get instances, devices, or device groups
                    return BuildErrorResponse("assignment-add", "Failed to get instances, devices, or device groups");
                }

                if (!string.IsNullOrEmpty(uuid))
                {
                    if (!devices.Any(x => x.Uuid == uuid))
                    {
                        // Device does not exist
                        return BuildErrorResponse("assignment-add", $"Device with name '{uuid}' does not exist");
                    }
                }

                if (!instances.Any(x => x.Name == destinationInstance))
                {
                    // Instance does not exist
                    return BuildErrorResponse("assignment-add", $"Instance with name '{destinationInstance}' does not exist");
                }

                if (!string.IsNullOrEmpty(deviceGroupName))
                {
                    if (!deviceGroups.Any(x => x.Name == deviceGroupName))
                    {
                        // Device group does not exist
                        return BuildErrorResponse("assignment-add", $"Device group with name '{deviceGroupName} does not exist");
                    }
                }

                var totalTime = 0u;
                if (!string.IsNullOrEmpty(time))
                {
                    var split = time.Split(':');
                    if (split.Length == 3)
                    {
                        var hours = int.Parse(split[0]);
                        var minutes = int.Parse(split[1]);
                        var seconds = int.Parse(split[2]);
                        var newTime = (hours * 3600) + (minutes * 60) + seconds;
                        totalTime = newTime == 0 ? 1 : (uint)newTime;
                    }
                    else
                    {
                        // Invalid time
                        return BuildErrorResponse("assignment-add", $"Invalid assignment time '{time}' provided");
                    }
                }
                DateTime? realDate = null;
                if (!string.IsNullOrEmpty(date))
                {
                    realDate = DateTime.Parse(date);
                }

                if (string.IsNullOrEmpty(destinationInstance))
                {
                    return BuildErrorResponse("assignment-add", $"No destination instance selected");
                }

                try
                {
                    var assignment = new Assignment
                    {
                        InstanceName = destinationInstance,
                        SourceInstanceName = sourceInstance,
                        DeviceUuid = uuid,
                        DeviceGroupName = deviceGroupName,
                        Time = totalTime,
                        Date = realDate,
                        Enabled = enabled,
                    };
                    await _assignmentRepository.AddAsync(assignment).ConfigureAwait(false);
                    AssignmentController.Instance.AddAssignment(assignment);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error: {ex}");
                }

                if (createOnComplete)
                {
                    var oncompleteAssignment = new Assignment
                    {
                        InstanceName = destinationInstance,
                        SourceInstanceName = sourceInstance,
                        DeviceUuid = uuid,
                        DeviceGroupName = deviceGroupName,
                        Time = 0,
                        Date = realDate,
                        Enabled = enabled,
                    };
                    await _assignmentRepository.AddAsync(oncompleteAssignment).ConfigureAwait(false);
                    AssignmentController.Instance.AddAssignment(oncompleteAssignment);
                }

                return Redirect("/dashboard/assignments");
            }
        }

        [
            HttpGet("/dashboard/assignment/edit/{id}"),
            HttpPost("/dashboard/assignment/edit/{id}"),
        ]
        public async Task<IActionResult> EditAssignment(uint id)
        {
            if (Request.Method == "GET")
            {
                var assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
                if (assignment == null)
                {
                    // Failed to get assignment by id, does assignment exist?
                    return BuildErrorResponse("assignment-edit", $"Failed to get assignment with id '{id}', does it exist?");
                }

                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                var deviceGroups = await _deviceGroupRepository.GetAllAsync().ConfigureAwait(false);
                if (devices == null || instances == null || deviceGroups == null)
                {
                    // Failed to get devices, instances, or device groups from database
                    return BuildErrorResponse("assignment-edit", $"Failed to get devices, instances, or device groups from database");                    
                }

                var formattedTime = assignment.Time == 0 ? "" : $"{assignment.Time / 3600:00}:{assignment.Time % 3600 / 60:00}:{assignment.Time % 3600 % 60:00}";
                dynamic obj = BuildDefaultData();
                obj.id = id;
                obj.old_name = id;
                obj.date = assignment.Date;
                obj.time = formattedTime;
                obj.enabled = assignment.Enabled ? "checked" : "";
                obj.instances = instances.Select(x => new { name = x.Name, selected = x.Name == assignment.InstanceName, selected_source = x.Name == assignment.SourceInstanceName });
                obj.devices = devices.Select(x => new { uuid = x.Uuid, selected = x.Uuid == assignment.DeviceUuid });
                obj.device_groups = deviceGroups.Select(x => new { uuid = x.Name, selected = x.Name == assignment.DeviceGroupName });
                var data = TemplateRenderer.ParseTemplate("assignment-edit", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var deviceOrDeviceGroupName = Request.Form["device"].ToString();
                var uuid = deviceOrDeviceGroupName.StartsWith("device:") ? new string(deviceOrDeviceGroupName.Skip(7).ToArray()) : null;
                var deviceGroupName = deviceOrDeviceGroupName.StartsWith("group:") ? new string(deviceOrDeviceGroupName.Skip(6).ToArray()) : null;
                var sourceInstance = Request.Form["source_instance"].ToString();
                sourceInstance = string.IsNullOrEmpty(sourceInstance) ? null : sourceInstance;
                var destinationInstance = Request.Form["instance"].ToString();
                var time = Request.Form["time"].ToString();
                var date = Request.Form["date"].ToString();
                var createOnComplete = Request.Form["oncomplete"].ToString() == "on";
                var enabled = Request.Form["enabled"].ToString() == "on";

                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                var deviceGroups = await _deviceGroupRepository.GetAllAsync().ConfigureAwait(false);
                if (instances == null || devices == null || deviceGroups == null)
                {
                    // Failed to get instances, devices, or device groups
                    return BuildErrorResponse("assignment-add", "Failed to get instances, devices, or device groups");
                }

                if (!string.IsNullOrEmpty(uuid))
                {
                    if (!devices.Any(x => x.Uuid == uuid))
                    {
                        // Device does not exist
                        return BuildErrorResponse("assignment-edit", $"Device with name '{uuid}' does not exist");
                    }
                }

                if (!instances.Any(x => string.Compare(x.Name, destinationInstance, true) == 0))
                {
                    // Instance does not exist
                    return BuildErrorResponse("assignment-edit", $"Instance with name '{destinationInstance}' does not exist");
                }

                if (!string.IsNullOrEmpty(deviceGroupName))
                {
                    if (!deviceGroups.Any(x => x.Name == deviceGroupName))
                    {
                        // Device group does not exist
                        return BuildErrorResponse("assignment-add", $"Device group with name '{deviceGroupName} does not exist");
                    }
                }

                var totalTime = 0u;
                if (!string.IsNullOrEmpty(time))
                {
                    var split = time.Split(':');
                    if (split.Length == 3)
                    {
                        var hours = int.Parse(split[0]);
                        var minutes = int.Parse(split[1]);
                        var seconds = int.Parse(split[2]);
                        var newTime = (hours * 3600) + (minutes * 60) + seconds;
                        totalTime = newTime == 0 ? 1 : (uint)newTime;
                    }
                    else
                    {
                        // Invalid time
                        return BuildErrorResponse("assignment-edit", $"Invalid assignment time '{time}' provided");
                    }
                }
                DateTime? realDate = null;
                if (!string.IsNullOrEmpty(date))
                {
                    realDate = DateTime.Parse(date);
                }

                if (string.IsNullOrEmpty(destinationInstance))
                {
                    // Invalid request, no destination instance selected
                    return BuildErrorResponse("assignment-add", $"No destination instance selected");
                }

                var assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
                if (assignment == null)
                {
                    // Failed to get assignment by id
                    return BuildErrorResponse("assignment-edit", $"Assignment with id '{id}' does not exist");
                }
                assignment.InstanceName = destinationInstance;
                assignment.SourceInstanceName = sourceInstance;
                assignment.DeviceUuid = uuid;
                assignment.DeviceGroupName = deviceGroupName;
                assignment.Time = totalTime;
                assignment.Date = realDate;
                assignment.Enabled = enabled;
                await _assignmentRepository.UpdateAsync(assignment).ConfigureAwait(false);
                AssignmentController.Instance.EditAssignment(assignment.Id, assignment);
                return Redirect("/dashboard/assignments");
            }
        }

        [HttpGet("/dashboard/assignment/start/{id}")]
        public async Task<IActionResult> StartAssignment(uint id)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (assignment != null)
            {
                await AssignmentController.Instance.TriggerAssignment(assignment, string.Empty, true).ConfigureAwait(false);
            }
            return Redirect("/dashboard/assignments");
        }

        [HttpGet("/dashboard/assignment/delete/{id}")]
        public async Task<IActionResult> DeleteAssignment(uint id)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (assignment != null)
            {
                await _assignmentRepository.DeleteAsync(assignment).ConfigureAwait(false);
            }
            return Redirect("/dashboard/assignments");
        }

        [HttpGet("/dashboard/assignments/delete_all")]
        public async Task<IActionResult> DeleteAllAssignments()
        {
            await _assignmentRepository.DeleteAllAsync().ConfigureAwait(false);
            return Redirect("/dashboard/assignments");
        }

        #endregion

        #region Webhooks

        [HttpGet("/dashboard/webhooks")]
        public IActionResult GetWebhooks()
        {
            var obj = BuildDefaultData();
            var data = TemplateRenderer.ParseTemplate("webhooks", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        [
            HttpGet("/dashboard/webhook/add"),
            HttpPost("/dashboard/webhook/add"),
        ]
        public async Task<IActionResult> AddWebhook()
        {
            if (Request.Method == "GET")
            {
                var geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
                dynamic obj = BuildDefaultData();
                obj.delay = 5;
                obj.types = Enum.GetValues(typeof(WebhookType)).Cast<WebhookType>().ToList().Select(x => new
                {
                    name = x.ToString(),
                    selected = false,
                });
                obj.geofences = geofences.Where(x => x.Type == GeofenceType.Geofence)
                                         .Select(x => new
                                         {
                                             name = x.Name,
                                             type = x.Type.ToString().ToLower(),
                                             selected = false,
                                         });
                obj.nothing_selected = true;
                var data = TemplateRenderer.ParseTemplate("webhook-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var name = Request.Form["name"].ToString();
                var types = Webhook.StringToWebhookTypes(Request.Form["types"]);
                var url = Request.Form["url"].ToString();
                var delay = double.Parse(Request.Form["delay"].ToString() ?? "5");
                var geofence = Request.Form["geofence"].ToString();
                var enabled = Request.Form["enabled"].ToString() == "on";
                var pokemonIds = NumberUtils.GenerateRange<uint>(Request.Form["pokemon_ids"].ToString(), 1, 999);
                var pokestopIds = Request.Form["pokestop_ids"].ToString()?.Split("\n").ToList();
                var raidIds = NumberUtils.GenerateRange<uint>(Request.Form["raid_ids"].ToString(), 1, 999);
                var eggLevels = NumberUtils.GenerateRange<ushort>(Request.Form["egg_ids"].ToString(), 1, 6);
                var lureIds = NumberUtils.GenerateRange<ushort>(Request.Form["lure_ids"].ToString(), 501, 504);
                var invasionIds = NumberUtils.GenerateRange<ushort>(Request.Form["invasion_ids"].ToString(), 1, 50);
                var gymIds = NumberUtils.GenerateRange<ushort>(Request.Form["gym_ids"].ToString(), 0, 3);
                var weatherIds = NumberUtils.GenerateRange<ushort>(Request.Form["weather_ids"].ToString(), 0, 7);

                if (types.Count == 0)
                {
                    // No webhook type selected (forgot if this is needed, double check lol)
                    return BuildErrorResponse("webhook-add", $"At least one webhook type needs to be selected");
                }

                // Make sure geofence exists
                var webhook = await _webhookRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (webhook != null)
                {
                    // Webhook already exists
                    return BuildErrorResponse("webhook-add", $"Webhook with name '{name}' already exists");
                }

                webhook = new Webhook
                {
                    Name = name,
                    Types = types,
                    Url = url,
                    Delay = delay,
                    Geofence = string.Compare(geofence, Strings.All, true) == 0 ? null : geofence,
                    Enabled = enabled,
                    Data = new WebhookData
                    {
                        PokemonIds = pokemonIds,
                        PokestopIds = pokestopIds,
                        RaidPokemonIds = raidIds,
                        EggLevels = eggLevels,
                        LureIds = lureIds,
                        InvasionIds = invasionIds,
                        GymTeamIds = gymIds,
                        WeatherConditionIds = weatherIds,
                    },
                };
                await _webhookRepository.AddAsync(webhook).ConfigureAwait(false);
                // Send redis webhook:reload event
                var webhooks = await _webhookRepository.GetAllAsync(false).ConfigureAwait(false);
                await PublishData(RedisChannels.WebhookReload, webhooks).ConfigureAwait(false);
                return Redirect("/dashboard/webhooks");
            }
        }

        [
            HttpGet("/dashboard/webhook/edit/{name}"),
            HttpPost("/dashboard/webhook/edit/{name}"),
        ]
        public async Task<IActionResult> EditWebhook(string name)
        {
            if (Request.Method == "GET")
            {
                var geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
                var webhook = await _webhookRepository.GetByIdAsync(name).ConfigureAwait(false);
                dynamic obj = BuildDefaultData();
                obj.name = webhook.Name;
                obj.old_name = webhook.Name;
                obj.url = webhook.Url;
                obj.delay = webhook.Delay;
                obj.types = Enum.GetValues(typeof(WebhookType)).Cast<WebhookType>().ToList().Select(x => new
                {
                    name = x.ToString(),
                    selected = webhook.Types.Contains(x),
                });
                obj.geofences = geofences.Where(x => x.Type == GeofenceType.Geofence)
                                         .Select(x => new
                                         {
                                             name = x.Name,
                                             type = x.Type.ToString().ToLower(),
                                             selected = string.Compare(x.Name, webhook.Geofence, true) == 0,
                                         });
                obj.pokemon_ids = string.Join("\n", webhook.Data.PokemonIds ?? new List<uint>());
                obj.pokestop_ids = string.Join("\n", webhook.Data.PokestopIds ?? new List<string>());
                obj.raid_ids = string.Join("\n", webhook.Data.RaidPokemonIds ?? new List<uint>());
                obj.egg_ids = string.Join("\n", webhook.Data.EggLevels ?? new List<ushort>());
                obj.lure_ids = string.Join("\n", webhook.Data.LureIds ?? new List<ushort>());
                obj.invasion_ids = string.Join("\n", webhook.Data.InvasionIds ?? new List<ushort>());
                obj.gym_ids = string.Join("\n", webhook.Data.GymTeamIds ?? new List<ushort>());
                obj.weather_ids = string.Join("\n", webhook.Data.WeatherConditionIds ?? new List<ushort>());
                obj.enabled = webhook.Enabled ? "checked" : "";
                obj.nothing_selected = string.IsNullOrEmpty(webhook.Geofence);
                var data = TemplateRenderer.ParseTemplate("webhook-edit", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                if (Request.Form.ContainsKey("delete"))
                {
                    var webhookToDelete = await _webhookRepository.GetByIdAsync(name).ConfigureAwait(false);
                    if (webhookToDelete != null)
                    {
                        await _webhookRepository.DeleteAsync(webhookToDelete).ConfigureAwait(false);
                        // Send redis webhook:reload event
                        var reloadWebhooks = await _webhookRepository.GetAllAsync(false).ConfigureAwait(false);
                        await PublishData(RedisChannels.WebhookReload, reloadWebhooks).ConfigureAwait(false);
                        _logger.LogDebug($"Webhook {name} was deleted");
                    }
                    return Redirect("/dashboard/webhooks");
                }

                var newName = Request.Form["name"].ToString();
                var types = Webhook.StringToWebhookTypes(Request.Form["types"]);
                var url = Request.Form["url"].ToString();
                var delay = double.Parse(Request.Form["delay"].ToString() ?? "5");
                var geofence = Request.Form["geofence"].ToString();
                var enabled = Request.Form["enabled"].ToString() == "on";
                var pokemonIds = NumberUtils.GenerateRange<uint>(Request.Form["pokemon_ids"].ToString(), 1, 999);
                var pokestopIds = Request.Form["pokestop_ids"].ToString()?.Split("\n").ToList();
                var raidIds = NumberUtils.GenerateRange<uint>(Request.Form["raid_ids"].ToString(), 1, 999);
                var eggLevels = NumberUtils.GenerateRange<ushort>(Request.Form["egg_ids"].ToString(), 1, 6);
                var lureIds = NumberUtils.GenerateRange<ushort>(Request.Form["lure_ids"].ToString(), 501, 504);
                var invasionIds = NumberUtils.GenerateRange<ushort>(Request.Form["invasion_ids"].ToString(), 1, 50);
                var gymIds = NumberUtils.GenerateRange<ushort>(Request.Form["gym_ids"].ToString(), 0, 3);
                var weatherIds = NumberUtils.GenerateRange<ushort>(Request.Form["weather_ids"].ToString(), 0, 7);

                if (types.Count == 0)
                {
                    // No webhook type selected (forgot if this is needed, double check lol)
                    return BuildErrorResponse("webhook-edit", $"At least one webhook type needs to be selected");
                }

                // Make sure geofence exists
                var webhook = await _webhookRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (webhook == null)
                {
                    // Webhook does not exist
                    return BuildErrorResponse("webhook-edit", $"Webhook with name '{name}' does not exist");
                }

                webhook.Name = name;
                webhook.Types = types;
                webhook.Url = url;
                webhook.Delay = delay;
                webhook.Geofence = string.Compare(geofence, Strings.All, true) == 0 ? null : geofence;
                webhook.Enabled = enabled;
                webhook.Data = new WebhookData
                {
                    PokemonIds = pokemonIds,
                    PokestopIds = pokestopIds,
                    RaidPokemonIds = raidIds,
                    EggLevels = eggLevels,
                    LureIds = lureIds,
                    InvasionIds = invasionIds,
                    GymTeamIds = gymIds,
                    WeatherConditionIds = weatherIds,
                };
                await _webhookRepository.UpdateAsync(webhook).ConfigureAwait(false);
                // Send redis webhook:reload event
                var webhooks = await _webhookRepository.GetAllAsync(false).ConfigureAwait(false);
                await PublishData(RedisChannels.WebhookReload, webhooks).ConfigureAwait(false);
                return Redirect("/dashboard/webhooks");
            }
        }

        #endregion

        #region Accounts

        [HttpGet("/dashboard/accounts")]
        public async Task<IActionResult> GetAccounts()
        {
            var stats = await _accountRepository.GetStatsAsync().ConfigureAwait(false);
            dynamic obj = BuildDefaultData();
            obj.stats = stats;
            var data = TemplateRenderer.ParseTemplate("accounts", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        [
            HttpGet("/dashboard/accounts/add"),
            HttpPost("/dashboard/accounts/add"),
        ]
        public async Task<IActionResult> AddAccounts()
        {
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                obj.level = 0;
                var data = TemplateRenderer.ParseTemplate("accounts-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                // Add accounts to database
                var level = ushort.Parse(Request.Form["level"].ToString());
                var group = Request.Form["group"].ToString();
                var accountsToAdd = Request.Form["accounts"].ToString();
                var rows = accountsToAdd.Replace(";", ",")
                                        .Replace(":", ",")
                                        .Split('\n');
                var list = new List<Account>();
                foreach (var row in rows)
                {
                    var split = row.Split(',');
                    if (split.Length != 2)
                    {
                        // Invalid account provided
                        continue;
                    }
                    list.Add(new Account
                    {
                        Username = split[0].Trim(),
                        Password = split[1].Trim(),
                        Level = level,
                        GroupName = group,
                    });
                }
                await _accountRepository.AddOrUpdateAsync(list).ConfigureAwait(false);
                return Redirect("/dashboard/accounts");
            }
        }

        #endregion

        #region Utilities

        [
            HttpGet("/dashboard/utilities"),
            HttpPost("/dashboard/utilities"),
        ]
        public async Task<IActionResult> Utilities()
        {
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                obj.stale_pokestops = (await _pokestopRepository.GetStalePokestopsCount().ConfigureAwait(false)).ToString("N0");
                obj.convertible_pokestops = (await _pokestopRepository.GetConvertiblePokestopsCount().ConfigureAwait(false)).ToString("N0");
                obj.warnings = (await _accountRepository.GetExpiredWarningsCount().ConfigureAwait(false)).ToString("N0");
                obj.bans = (await _accountRepository.GetExpiredBansCount().ConfigureAwait(false)).ToString("N0");
                var data = TemplateRenderer.ParseTemplate("utilities", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var type = Request.Form["action"].ToString();
                switch (type)
                {
                    case "delete_stale_pokestops":
                        var stopsDeleted = await _pokestopRepository.DeleteStalePokestops().ConfigureAwait(false);
                        return BuildSuccessResponse("utilities", $"<b>{stopsDeleted}</b> Stale Pokestops deleted");
                    case "clear_expired_bans":
                        var bansCleared = await _accountRepository.ClearExpiredBans().ConfigureAwait(false);
                        return BuildSuccessResponse("utilities", $"<b>{bansCleared}</b> Expired bans cleared");
                    case "clear_expired_warnings":
                        var warningsCleared = await _accountRepository.ClearExpiredWarnings().ConfigureAwait(false);
                        return BuildSuccessResponse("utilities", $"<b>{warningsCleared}</b> Expired warnings cleared");
                    case "truncate_pokemon":
                        await _pokemonRepository.Truncate().ConfigureAwait(false);
                        return BuildSuccessResponse("utilities", "Pokemon table successfully truncated");
                    case "convert_pokestops":
                        // TODO: Update gyms with pokestop names and urls if set
                        //var stopsConverted = await _pokestopRepository.ConvertPokestopsToGyms().ConfigureAwait(false);
                        // - delete converted pokestops
                        // - Pass data through to view
                        return BuildSuccessResponse("utilities", $"<b>0</b> Pokestops converted to Gyms");
                    case "force_logout_all_devices":
                        await _deviceRepository.ClearAllAccounts().ConfigureAwait(false);
                        return BuildSuccessResponse("utilities", $"All devices forced to logout");
                    case "clear_quests":
                        await _pokestopRepository.ClearQuestsAsync().ConfigureAwait(false);
                        return BuildSuccessResponse("utilities", "All Pokestop quests have been cleared");
                    case "clear_iv_queues":
                        break;
                    case "flush_redis":
                        var result = await _redisDatabase.ExecuteAsync("FLUSHDB").ConfigureAwait(false);
                        if (string.Compare(result.ToString(), "OK", true) == 0)
                            return BuildSuccessResponse("utilities", "Redis database successfully flushed");
                        return BuildErrorResponse("utilities", $"Failed to flush Redis database: {result?.Type}");
                }
                return Redirect("/dashboard/utilities");
            }
        }

        #endregion

        [
            HttpGet("/dashboard/settings"),
            HttpPost("/dashboard/settings"),
        ]
        public IActionResult GetSettings()
        {
            if (Request.Method == "GET")
            {
                var obj = BuildDefaultData();
                var data = TemplateRenderer.ParseTemplate("settings", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                return Redirect("/dashboard/settings");
            }
        }

        [HttpGet("/dashboard/about")]
        public IActionResult GetAbout()
        {
            var obj = BuildDefaultData();
            var data = TemplateRenderer.ParseTemplate("about", obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        #endregion

        #region Helper Methods

        private static CircleRouteType StringToCircleRouteType(string type)
        {
            return (type.ToLower()) switch
            {
                "leapfrog" => CircleRouteType.Default,
                "spread" => CircleRouteType.Split,
                "circular" => CircleRouteType.Circular,
                _ => CircleRouteType.Default,
            };
        }

        private static string CircleRouteTypeToString(CircleRouteType type)
        {
            return type switch
            {
                CircleRouteType.Default => "leapfrog",
                CircleRouteType.Split => "spread",
                CircleRouteType.Circular => "circular",
                _ => "leapfrog",
            };
        }

        private static ExpandoObject BuildDefaultData()
        {
            // TODO: Include locales
            dynamic obj = new ExpandoObject();
            obj.started = Strings.Started;
            obj.title = "Chuck Device Controller";
            obj.locale = "en";
            obj.locale_new = "en";
            obj.body_class = "theme-dark";
            obj.table_class = "table-dark";
            obj.current_version = Assembly.GetExecutingAssembly().GetName().Version;
            return obj;
        }

        private static IActionResult BuildErrorResponse(string template, string message)
        {
            dynamic obj = BuildDefaultData();
            obj.show_error = true;
            obj.error = message;
            var data = TemplateRenderer.ParseTemplate(template, obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        private static IActionResult BuildSuccessResponse(string template, string message)
        {
            dynamic obj = BuildDefaultData();
            obj.show_success = true;
            obj.success = message;
            var data = TemplateRenderer.ParseTemplate(template, obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        private Task PublishData<T>(string channel, T data)
        {
            try
            {
                if (data == null)
                {
                    return Task.CompletedTask;
                }
                _subscriber.PublishAsync(channel, data.ToJson(), CommandFlags.FireAndForget);
            }
            catch (Exception ex)
            {
                _logger.LogError($"PublishData: {ex}");
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}