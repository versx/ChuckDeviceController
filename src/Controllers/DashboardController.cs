﻿namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using POGOProtos.Rpc;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.JobControllers;
    using ChuckDeviceController.JobControllers.Instances;
    using ChuckDeviceController.Net.Webhooks;
    using ChuckDeviceController.Utilities;

    [Controller]
    [Route("/dashboard")]
    public class DashboardController : Controller
    {
        #region Variables

        // Dependency injection variables
        private readonly DeviceControllerContext _context;
        private readonly ILogger<DeviceController> _logger;

        private readonly AccountRepository _accountRepository;
        private readonly AssignmentRepository _assignmentRepository;
        private readonly DeviceRepository _deviceRepository;
        private readonly InstanceRepository _instanceRepository;
        private readonly PokestopRepository _pokestopRepository;
        private readonly GeofenceRepository _geofenceRepository;
        private readonly WebhookRepository _webhookRepository;

        #endregion

        #region Constructor

        public DashboardController(DeviceControllerContext context, ILogger<DeviceController> logger)
        {
            _context = context;
            _logger = logger;

            _accountRepository = new AccountRepository(_context);
            _assignmentRepository = new AssignmentRepository(_context);
            _deviceRepository = new DeviceRepository(_context);
            _instanceRepository = new InstanceRepository(_context);
            _pokestopRepository = new PokestopRepository(_context);
            _geofenceRepository = new GeofenceRepository(_context);
            _webhookRepository = new WebhookRepository(_context);
        }

        #endregion

        #region Routes

        [HttpGet("/")]
        public IActionResult GetIndex() => Redirect("/dashboard");

        [HttpGet("/dashboard")]
        public IActionResult GetDashboard()
        {
            dynamic obj = BuildDefaultData();
            obj.devices_count = _context.Devices.Count().ToString("N0");
            obj.instances_count = _context.Instances.Count().ToString("N0");
            obj.assignments_count = _context.Assignments.Count().ToString("N0");
            obj.accounts_count = _context.Accounts.Count().ToString("N0");
            obj.geofences_count = _context.Geofences.Count().ToString("N0");
            obj.webhooks_count = _context.Webhooks.Count().ToString("N0");
            var data = Renderer.ParseTemplate("index", obj);
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
            var data = Renderer.ParseTemplate("devices", obj);
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
                var data = Renderer.ParseTemplate("device-assign", obj);
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

        #region Instances

        [HttpGet("/dashboard/instances")]
        public IActionResult GetInstances()
        {
            var obj = BuildDefaultData();
            var data = Renderer.ParseTemplate("instances", obj);
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
                obj.nothing_selected = true;
                obj.timezone_offset = 0;
                obj.circle_size = 70;
                obj.nothing_selected = true;
                obj.iv_queue_limit = 100;
                obj.spin_limit = 3500;
                var data = Renderer.ParseTemplate("instance-add", obj);
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
                var timezoneOffset = Request.Form.ContainsKey("timezone_offset")
                    ? int.Parse(Request.Form["timezone_offset"].ToString() ?? "0")
                    : 0;
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
                ushort ivQueueLimit = 100;
                ushort spinLimit = 3500;
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
                }
                //var scatterPokemonIds = Request.Form["scatter_pokemon_ids"];
                //var accountGroup = Request.Form["account_group"];
                //var isEvent = Request.Form["is_event"];
                if (minLevel > maxLevel || minLevel == 0 || minLevel > 40 || maxLevel == 0 || maxLevel > 40)
                {
                    // Invalid levels
                    return BuildErrorResponse("instance-add", $"Invalid minimum and maximum levels provided");
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
                        IsEvent = false,
                        IVQueueLimit = ivQueueLimit,
                        SpinLimit = spinLimit,
                        MinimumLevel = minLevel,
                        MaximumLevel = maxLevel,
                        PokemonIds = pokemonIds,
                        TimezoneOffset = timezoneOffset,
                        CircleRouteType = circleRouteType,
                        CircleSize = (ushort)circleSize,
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
                obj.circle_pokemon_selected = instance.Type == InstanceType.CirclePokemon;
                obj.circle_raid_selected = instance.Type == InstanceType.CircleRaid;
                obj.pokemon_iv_selected = instance.Type == InstanceType.PokemonIV;
                obj.auto_quest_selected = instance.Type == InstanceType.AutoQuest;
                obj.bootstrap_selected = instance.Type == InstanceType.Bootstrap;
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
                        obj.circle_route_type = instance.Data.CircleRouteType; // TODO: ToString
                //        break;
                //    case InstanceType.PokemonIV:
                        obj.pokemon_ids = instance.Data.PokemonIds == null ? null : string.Join("\n", instance.Data.PokemonIds);
                        obj.iv_queue_limit = instance.Data.IVQueueLimit > 0 ? instance.Data.IVQueueLimit : 100;
                //        break;
                //    case InstanceType.AutoQuest:
                        obj.timezone_offset = instance.Data.TimezoneOffset ?? 0;
                        obj.spin_limit = instance.Data.SpinLimit > 0 ? instance.Data.SpinLimit : 3500;
                //        break;
                //    case InstanceType.Bootstrap:
                        obj.circle_size = instance.Data.CircleSize ?? 70;
                //}
                var data = Renderer.ParseTemplate("instance-edit", obj);
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

                var newName = Request.Form["name"].ToString();
                var type = Instance.StringToInstanceType(Request.Form["type"]);
                var geofence = Request.Form["geofence"].ToString();
                //var area = Request.Form["area"].ToString();
                var minLevel = ushort.Parse(Request.Form["min_level"]);
                var maxLevel = ushort.Parse(Request.Form["max_level"]);
                var timezoneOffset = int.Parse(Request.Form["timezone_offset"].ToString() ?? "0");
                var circleSize = ushort.Parse(Request.Form["circle_Size"].ToString() ?? "70");
                var circleRouteType = Request.Form.ContainsKey("circle_route_type")
                    ? StringToCircleRouteType(Request.Form["circle_route_type"])
                    : CircleRouteType.Default;
                var pokemonIdsValue = Request.Form["pokemon_ids"].ToString();
                var pokemonIds = pokemonIdsValue == "*"
                    ? Enumerable.Range(1, 999).Select(x => (uint)x).ToList()
                    : pokemonIdsValue?.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)?.Select(uint.Parse).ToList();
                //var scatterPokemonIds = Request.Form["scatter_pokemon_ids"];
                var ivQueueLimit = ushort.Parse(Request.Form["iv_queue_limit"]);
                var spinLimit = ushort.Parse(Request.Form["spin_limit"]);
                //var accountGroup = Request.Form["account_group"];
                //var isEvent = Request.Form["is_event"];
                if (minLevel > maxLevel || minLevel == 0 || minLevel > 40 || maxLevel == 0 || maxLevel > 40)
                {
                    // Invalid levels
                    return BuildErrorResponse("instance-edit", $"Invalid minimum and maximum levels provided");
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
                    IsEvent = false,
                    IVQueueLimit = ivQueueLimit,
                    SpinLimit = spinLimit,
                    MinimumLevel = minLevel,
                    MaximumLevel = maxLevel,
                    PokemonIds = pokemonIds,
                    TimezoneOffset = timezoneOffset,
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
            var data = Renderer.ParseTemplate("instance-ivqueue", obj);
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
            var data = Renderer.ParseTemplate("geofences", obj);
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
            // TODO: Reload instances upon geofence change
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                var data = Renderer.ParseTemplate("geofence-add", obj);
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
                            var rows = area.Split('\n');
                            var coords = new List<Coordinate>();
                            foreach (var row in rows)
                            {
                                var split = row.Split(',');
                                if (split.Length != 2)
                                    continue;
                                coords.Add(new Coordinate(double.Parse(split[0]), double.Parse(split[1])));
                            }
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                    case GeofenceType.Geofence:
                        {
                            var rows = area.Split('\n');
                            var index = 0;
                            var coords = new List<List<Coordinate>> { new List<Coordinate>() };
                            foreach (var row in rows)
                            {
                                var split = row.Split(',');
                                if (split.Length == 2)
                                {
                                    coords[index].Add(new Coordinate(double.Parse(split[0]), double.Parse(split[1])));
                                }
                                else if (row.Contains("[") && row.Contains("]") && coords.Count > index && coords[index].Count > 0)
                                {
                                    index++;
                                    coords.Add(new List<Coordinate>());
                                }
                            }
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
                return Redirect("/dashboard/geofences");
            }
        }

        [
            HttpGet("/dashboard/geofence/edit/{name}"),
            HttpPost("/dashboard/geofence/edit/{name}"),
        ]
        public async Task<IActionResult> EditGeofence(string name)
        {
            // TODO: Reload instances upon geofence change
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                var geofence = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (geofence == null)
                {
                    // Provided geofence name does not exist
                    // TODO: Log error
                    return Redirect("/dashboard/geofences");
                }
                obj.name = geofence.Name;
                obj.old_name = geofence.Name;
                obj.circle_selected = geofence.Type == GeofenceType.Circle;
                obj.geofence_selected = geofence.Type == GeofenceType.Geofence;
                var coords = string.Empty;
                var coordsArray = geofence.Data.Area;
                if (geofence.Type == GeofenceType.Circle)
                {
                    coords = CoordinatesToAreaString(coordsArray);
                }
                else if (geofence.Type == GeofenceType.Geofence)
                {
                    coords = MultiPolygonToAreaString(coordsArray);
                }
                obj.area = coords;
                var data = Renderer.ParseTemplate("geofence-edit", obj);
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
                            var coords = AreaStringToCoordinates(area);
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                    case GeofenceType.Geofence:
                        {
                            var coords = AreaStringToMultiPolygon(area);
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
                return Redirect("/dashboard/geofences");
            }
        }

        #endregion

        #region Assignments

        [HttpGet("/dashboard/assignments")]
        public IActionResult GetAssignments()
        {
            var obj = BuildDefaultData();
            var data = Renderer.ParseTemplate("assignments", obj);
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
                dynamic obj = BuildDefaultData();
                obj.instances = instances.Select(x => new { name = x.Name, selected = false, selected_source = false });
                obj.devices = devices.Select(x => new { uuid = x.Uuid, selected = false });
                obj.nothing_selected = true;
                obj.nothing_selected_source = true;
                var data = Renderer.ParseTemplate("assignment-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var uuid = Request.Form["device"].ToString();
                var sourceInstance = Request.Form["source_instance"].ToString();
                var destinationInstance = Request.Form["instance"].ToString();
                var time = Request.Form["time"].ToString();
                var date = Request.Form["date"].ToString();
                var createOnComplete = Request.Form["oncomplete"].ToString() == "on";
                var enabled = Request.Form["enabled"].ToString() == "on";

                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                if (instances == null || devices == null)
                {
                    // Failed to get instances and/or devices, or no instances or devices in database
                    return BuildErrorResponse("assignment-add", $"Failed to get instances or devices");
                }

                if (!devices.Any(x => x.Uuid == uuid))
                {
                    // Device does not exist
                    return BuildErrorResponse("assignment-add", $"Device with name '{uuid}' does not exist");
                }

                if (!instances.Any(x => string.Compare(x.Name, destinationInstance, true) == 0))
                {
                    // Instance does not exist
                    return BuildErrorResponse("assignment-add", $"Instance with name '{destinationInstance}' does not exist");
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

                if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(destinationInstance))
                {
                    // TODO: Log error
                    return Redirect("/dashboard/assignments");
                    //return BuildErrorResponse("assignment-add", $"Somehow device uuid was null or destinationInstance");
                }

                try
                {
                    var assignment = new Assignment
                    {
                        InstanceName = destinationInstance,
                        SourceInstanceName = sourceInstance,
                        DeviceUuid = uuid,
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
                }

                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                if (devices == null || instances == null)
                {
                    // Failed to get devices or instances from database
                }

                var formattedTime = assignment.Time == 0 ? "" : $"{assignment.Time / 3600:00}:{assignment.Time % 3600 / 60:00}:{assignment.Time % 3600 % 60:00}";
                dynamic obj = BuildDefaultData();
                obj.id = id;
                obj.date = assignment.Date;
                obj.time = formattedTime;
                obj.enabled = assignment.Enabled ? "checked" : "";
                obj.instances = instances.Select(x => new { name = x.Name, selected = x.Name == assignment.InstanceName, selected_source = x.Name == assignment.SourceInstanceName });
                obj.devices = devices.Select(x => new { uuid = x.Uuid, selected = x.Uuid == assignment.DeviceUuid });
                var data = Renderer.ParseTemplate("assignment-edit", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                var uuid = Request.Form["device"].ToString();
                var sourceInstance = Request.Form["source_instance"].ToString();
                var destinationInstance = Request.Form["instance"].ToString();
                var time = Request.Form["time"].ToString();
                var date = Request.Form["date"].ToString();
                var createOnComplete = Request.Form["oncomplete"].ToString() == "on";
                var enabled = Request.Form["enabled"].ToString() == "on";

                var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                if (instances == null || devices == null)
                {
                    // Failed to get instances and/or devices, or no instances or devices in database
                    return BuildErrorResponse("assignment-edit", $"Failed to get instances or devices");
                }

                if (!devices.Any(x => x.Uuid == uuid))
                {
                    // Device does not exist
                    return BuildErrorResponse("assignment-edit", $"Device with name '{uuid}' does not exist");
                }

                if (!instances.Any(x => string.Compare(x.Name, destinationInstance, true) == 0))
                {
                    // Instance does not exist
                    return BuildErrorResponse("assignment-edit", $"Instance with name '{destinationInstance}' does not exist");
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

                if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(destinationInstance))
                {
                    // Invalid request, uuid or instance are null
                    // TODO: Log error
                    return Redirect("/dashboard/assignments");
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
            if (assignment == null)
            {
                // TODO: Log error
                // Failed to get assignment by id
            }
            await AssignmentController.Instance.TriggerAssignment(assignment, true).ConfigureAwait(false);
            return Redirect("/dashboard/assignments");
        }

        [HttpGet("/dashboard/assignment/delete/{id}")]
        public async Task<IActionResult> DeleteAssignment(uint id)
        {
            var assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
            if (assignment == null)
            {
                // TODO: Log error
                // Failed to delete assignment by id, does it exist?
                return BuildErrorResponse("assignment-delete", $"Assignment with id '{id}' does not exist");
            }
            await _assignmentRepository.DeleteAsync(assignment).ConfigureAwait(false);
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
            var data = Renderer.ParseTemplate("webhooks", obj);
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
                var data = Renderer.ParseTemplate("webhook-add", obj);
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
                var pokemonIds = GenerateRange<uint>(Request.Form["pokemon_ids"].ToString(), 1, 999);
                var pokestopIds = Request.Form["pokestop_ids"].ToString()?.Split("\n").ToList();
                var raidIds = GenerateRange<uint>(Request.Form["raid_ids"].ToString(), 1, 999);
                var eggLevels = GenerateRange<ushort>(Request.Form["egg_ids"].ToString(), 1, 6);
                var lureIds = GenerateRange<Item>(Request.Form["lure_ids"].ToString(), 501, 504);
                var invasionIds = GenerateRange<InvasionCharacter>(Request.Form["invasion_ids"].ToString(), 1, 50);
                var gymIds = GenerateRange<Team>(Request.Form["gym_ids"].ToString(), 0, 3);
                var weatherIds = GenerateRange<WeatherCondition>(Request.Form["weather_ids"].ToString(), 0, 7);

                if (types.Count == 0)
                {
                    // No webhook type selected (forgot if this is needed, double check lol)
                    return BuildErrorResponse("webhook-add", $"At least one webhook type needs to be selected");
                }

                // TODO: Make sure geofence exists

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
                    Geofence = geofence,
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
                await WebhookController.Instance.Reload().ConfigureAwait(false);
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
                    selected = false,
                });
                // TODO: Pokemon/raid/egg/etc ids
                obj.pokemon_ids = string.Join("\n", webhook.Data.PokemonIds ?? new List<uint>());
                obj.pokestop_ids = string.Join("\n", webhook.Data.PokestopIds ?? new List<string>());
                obj.raid_ids = string.Join("\n", webhook.Data.RaidPokemonIds ?? new List<uint>());
                obj.egg_ids = string.Join("\n", webhook.Data.EggLevels ?? new List<ushort>());
                obj.lure_ids = string.Join("\n", webhook.Data.LureIds ?? new List<Item>());
                obj.invasion_ids = string.Join("\n", webhook.Data.InvasionIds ?? new List<InvasionCharacter>());
                obj.gym_ids = string.Join("\n", webhook.Data.GymTeamIds ?? new List<Team>());
                obj.weather_ids = string.Join("\n", webhook.Data.WeatherConditionIds ?? new List<WeatherCondition>());
                obj.enabled = webhook.Enabled ? "checked" : "";
                var data = Renderer.ParseTemplate("webhook-edit", obj);
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
                        await WebhookController.Instance.Reload().ConfigureAwait(false);
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
                var pokemonIds = GenerateRange<uint>(Request.Form["pokemon_ids"].ToString(), 1, 999);
                var pokestopIds = Request.Form["pokestop_ids"].ToString()?.Split("\n").ToList();
                var raidIds = GenerateRange<uint>(Request.Form["raid_ids"].ToString(), 1, 999);
                var eggLevels = GenerateRange<ushort>(Request.Form["egg_ids"].ToString(), 1, 6);
                var lureIds = GenerateRange<Item>(Request.Form["lure_ids"].ToString(), 501, 504);
                var invasionIds = GenerateRange<InvasionCharacter>(Request.Form["invasion_ids"].ToString(), 1, 50);
                var gymIds = GenerateRange<Team>(Request.Form["gym_ids"].ToString(), 0, 3);
                var weatherIds = GenerateRange<WeatherCondition>(Request.Form["weather_ids"].ToString(), 0, 7);

                if (types.Count == 0)
                {
                    // No webhook type selected (forgot if this is needed, double check lol)
                    return BuildErrorResponse("webhook-edit", $"At least one webhook type needs to be selected");
                }

                // TODO: Make sure geofence exists

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
                webhook.Geofence = geofence;
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
                await WebhookController.Instance.Reload().ConfigureAwait(false);
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
            var data = Renderer.ParseTemplate("accounts", obj);
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
                var data = Renderer.ParseTemplate("accounts-add", obj);
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
                    });
                }
                await _accountRepository.AddOrUpdateAsync(list).ConfigureAwait(false);
                return Redirect("/dashboard/accounts");
            }
        }

        #endregion

        #region Utilities

        [
            HttpGet("/dashboard/clearquests"),
            HttpPost("/dashboard/clearquests"),
        ]
        public async Task<IActionResult> ClearQuests()
        {
            if (Request.Method == "GET")
            {
                var obj = BuildDefaultData();
                var data = Renderer.ParseTemplate("clearquests", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                await _pokestopRepository.ClearQuestsAsync().ConfigureAwait(false);
                return Redirect("/dashboard");
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
                var data = Renderer.ParseTemplate("settings", obj);
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

        #endregion

        #region Helper Methods

        private static CircleRouteType StringToCircleRouteType(string type)
        {
            return (type.ToLower()) switch
            {
                "leapfrog" => CircleRouteType.Default,
                "spread"   => CircleRouteType.Split,
                "circular" => CircleRouteType.Circular,
                _ => CircleRouteType.Default,
            };
        }

        private static string CircleRouteTypeToString(CircleRouteType type)
        {
            return type switch
            {
                CircleRouteType.Default =>  "leapfrog",
                CircleRouteType.Split =>    "spread",
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
            obj.current_version = "0.1.0";
            return obj;
        }

        private static string CoordinatesToAreaString(dynamic area)
        {
            var coords = string.Empty;
            foreach (var coord in area.EnumerateArray())
            {
                coords += $"{coord.GetProperty("lat").GetDouble()},{coord.GetProperty("lon").GetDouble()}\n";
            }
            return coords;
        }

        private static string MultiPolygonToAreaString(dynamic area)
        {
            var index = 1;
            var coords = string.Empty;
            foreach (var fence in area.EnumerateArray())
            {
                coords += $"[Geofence {index}]\n";
                foreach (var coord in fence.EnumerateArray())
                {
                    coords += $"{coord.GetProperty("lat").GetDouble()},{coord.GetProperty("lon").GetDouble()}\n";
                }
                index++;
            }
            return coords;
        }

        private static List<Coordinate> AreaStringToCoordinates(string area)
        {
            var rows = area.Split('\n');
            var coords = new List<Coordinate>();
            foreach (var row in rows)
            {
                var split = row.Split(',');
                if (split.Length != 2)
                    continue;
                var latitude = string.Format(CultureInfo.InvariantCulture, "{0:0.00000000}", split[0]);
                var longitude = string.Format(CultureInfo.InvariantCulture, "{0:0.00000000}", split[1]);
                coords.Add(new Coordinate(double.Parse(latitude), double.Parse(longitude)));
            }
            return coords;
        }

        private static List<List<Coordinate>> AreaStringToMultiPolygon(string area)
        {
            var rows = area.Split('\n');
            var index = 0;
            var coords = new List<List<Coordinate>> { new List<Coordinate>() };
            foreach (var row in rows)
            {
                var split = row.Split(',');
                if (split.Length == 2)
                {
                    var latitude = string.Format(CultureInfo.InvariantCulture, "{0:0.00000000}", split[0]);
                    var longitude = string.Format(CultureInfo.InvariantCulture, "{0:0.00000000}", split[1]);
                    coords[index].Add(new Coordinate(double.Parse(latitude), double.Parse(longitude)));
                }
                else if (row.Contains("[") && row.Contains("]") && coords.Count > index && coords[index].Count > 0)
                {
                    index++;
                    coords.Add(new List<Coordinate>());
                }
            }
            return coords;
        }

        private static IActionResult BuildErrorResponse(string template, string message)
        {
            dynamic obj = BuildDefaultData();
            obj.show_error = true;
            obj.error = message;
            var data = Renderer.ParseTemplate(template, obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        private static List<T> GenerateRange<T>(string ids, int min, int max)
        {
            if (string.IsNullOrEmpty(ids))
                return new List<T>();
            if (ids == "*")
            {
                return (List<T>)Enumerable.Range(min, max);
            }
            return (List<T>)ids.Split('\n').Select(int.Parse);

        }

        #endregion
    }
}