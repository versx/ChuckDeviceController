namespace ChuckDeviceController.Controllers
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.JobControllers;
    using ChuckDeviceController.JobControllers.Instances;
    using ChuckDeviceController.Utilities;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

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
        }

        #endregion

        #region Routes

        [HttpGet("/")]
        public IActionResult GetIndex()
        {
            return Redirect("/dashboard");
        }

        [HttpGet("/dashboard")]
        public IActionResult GetDashboard()
        {
            dynamic obj = BuildDefaultData();
            obj.devices_count = _context.Devices.Count().ToString("N0");
            obj.instances_count = _context.Instances.Count().ToString("N0");
            obj.assignments_count = _context.Assignments.Count().ToString("N0");
            obj.accounts_count = _context.Accounts.Count().ToString("N0");
            obj.geofences_count = _context.Geofences.Count().ToString("N0");
            dynamic data = Renderer.ParseTemplate("index", obj);
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
            ExpandoObject obj = BuildDefaultData();
            string data = Renderer.ParseTemplate("devices", obj);
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
            Device device = await _deviceRepository.GetByIdAsync(uuid).ConfigureAwait(false);
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
                dynamic data = Renderer.ParseTemplate("device-assign", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                Microsoft.Extensions.Primitives.StringValues instanceName = Request.Form["instance"];
                if (string.IsNullOrEmpty(instanceName))
                {
                    // Unknown instance name provided
                    return BuildErrorResponse("device-assign", $"Instance '{instanceName}' does not exist");
                }
                Instance instance = await _instanceRepository.GetByIdAsync(instanceName).ConfigureAwait(false);
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
            ExpandoObject obj = BuildDefaultData();
            string data = Renderer.ParseTemplate("instances", obj);
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
                IReadOnlyList<Geofence> geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
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
                dynamic data = Renderer.ParseTemplate("instance-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                string name = Request.Form["name"].ToString();
                InstanceType type = Instance.StringToInstanceType(Request.Form["type"]);
                string geofence = Request.Form["geofence"].ToString();
                string area = Request.Form["area"].ToString();
                ushort minLevel = ushort.Parse(Request.Form["min_level"]);
                ushort maxLevel = ushort.Parse(Request.Form["max_level"]);
                int timezoneOffset = Request.Form.ContainsKey("timezone_offset")
                    ? int.Parse(Request.Form["timezone_offset"].ToString() ?? "0")
                    : 0;
                CircleRouteType circleRouteType = Request.Form.ContainsKey("circle_route_type")
                    ? StringToCircleRouteType(Request.Form["circle_route_type"])
                    : CircleRouteType.Default;
                int circleSize = Request.Form.ContainsKey("circle_size")
                    ? ushort.Parse(Request.Form["circle_size"].ToString() ?? "70")
                    : 70;
                string pokemonIdsValue = Request.Form["pokemon_ids"].ToString();
                List<uint> pokemonIds = pokemonIdsValue == "*"
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

                if (await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false) != null)
                {
                    // Instance already exists
                    return BuildErrorResponse("instance-add", $"Instance with name '{name}' already exists");
                }

                Instance instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
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
                Instance instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (instance == null)
                {
                    // Failed to get instance by name
                    return BuildErrorResponse("instance-edit", $"Instance with name '{name}' does not exist");
                }
                ushort minLevel = instance.Data.MinimumLevel;
                ushort maxLevel = instance.Data.MaximumLevel;
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
                IReadOnlyList<Geofence> geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
                obj.geofences = geofences.Select(x => new
                {
                    name = x.Name,
                    type = x.Type.ToString().ToLower(),
                    selected = string.Compare(instance.Geofence, x.Name, true) == 0,
                });
                Geofence geofence = geofences.FirstOrDefault(x => string.Compare(x.Name, instance.Geofence, true) == 0);
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
                dynamic data = Renderer.ParseTemplate("instance-edit", obj);
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
                    Instance instanceToDelete = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                    if (instanceToDelete != null)
                    {
                        await _instanceRepository.DeleteAsync(instanceToDelete).ConfigureAwait(false);
                        await InstanceController.Instance.RemoveInstance(name).ConfigureAwait(false);
                        _logger.LogDebug($"Instance {name} was deleted");
                    }
                    return Redirect("/dashboard/instances");
                }

                string newName = Request.Form["name"].ToString();
                InstanceType type = Instance.StringToInstanceType(Request.Form["type"]);
                string geofence = Request.Form["geofence"].ToString();
                //var area = Request.Form["area"].ToString();
                ushort minLevel = ushort.Parse(Request.Form["min_level"]);
                ushort maxLevel = ushort.Parse(Request.Form["max_level"]);
                int timezoneOffset = int.Parse(Request.Form["timezone_offset"].ToString() ?? "0");
                ushort circleSize = ushort.Parse(Request.Form["circle_Size"].ToString() ?? "70");
                CircleRouteType circleRouteType = Request.Form.ContainsKey("circle_route_type")
                    ? StringToCircleRouteType(Request.Form["circle_route_type"].ToString())
                    : CircleRouteType.Default;
                string pokemonIdsValue = Request.Form["pokemon_ids"].ToString();
                List<uint> pokemonIds = pokemonIdsValue == "*"
                    ? Enumerable.Range(1, 999).Select(x => (uint)x).ToList()
                    : pokemonIdsValue?.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)?.Select(uint.Parse).ToList();
                //var scatterPokemonIds = Request.Form["scatter_pokemon_ids"];
                ushort ivQueueLimit = ushort.Parse(Request.Form["iv_queue_limit"]);
                ushort spinLimit = ushort.Parse(Request.Form["spin_limit"]);
                //var accountGroup = Request.Form["account_group"];
                //var isEvent = Request.Form["is_event"];
                if (minLevel > maxLevel || minLevel == 0 || minLevel > 40 || maxLevel == 0 || maxLevel > 40)
                {
                    // Invalid levels
                    return BuildErrorResponse("instance-edit", $"Invalid minimum and maximum levels provided");
                }

                Instance instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
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
                    CircleRouteType = circleRouteType,
                    CircleSize = circleSize,
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
            dynamic data = Renderer.ParseTemplate("instance-ivqueue", obj);
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
            ExpandoObject obj = BuildDefaultData();
            string data = Renderer.ParseTemplate("geofences", obj);
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
                dynamic data = Renderer.ParseTemplate("geofence-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                string name = Request.Form["name"].ToString();
                GeofenceType type = Request.Form["type"].ToString() == "circle"
                    ? GeofenceType.Circle
                    : GeofenceType.Geofence;
                string area = Request.Form["area"].ToString();

                Geofence geofence = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
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
                            List<Coordinate> coords = AreaStringToCoordinates(area);
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                    case GeofenceType.Geofence:
                        {
                            List<List<Coordinate>> coords = AreaStringToMultiPolygon(area);
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
                Geofence geofence = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
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
                string coords = string.Empty;
                dynamic coordsArray = geofence?.Data?.Area;
                if (geofence.Type == GeofenceType.Circle)
                {
                    coords = CoordinatesToAreaString(coordsArray);
                }
                else if (geofence.Type == GeofenceType.Geofence)
                {
                    coords = MultiPolygonToAreaString(coordsArray);
                }
                obj.area = coords;
                dynamic data = Renderer.ParseTemplate("geofence-edit", obj);
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
                    Geofence geofenceToDelete = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
                    if (geofenceToDelete != null)
                    {
                        await _geofenceRepository.DeleteAsync(geofenceToDelete).ConfigureAwait(false);
                        _logger.LogDebug($"Geofence {name} was deleted");
                    }
                    return Redirect("/dashboard/geofences");
                }

                string newName = Request.Form["name"].ToString();
                GeofenceType type = Request.Form["type"].ToString() == "circle"
                    ? GeofenceType.Circle
                    : GeofenceType.Geofence;
                string area = Request.Form["area"].ToString();

                Geofence geofence = await _geofenceRepository.GetByIdAsync(name).ConfigureAwait(false);
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
                            List<Coordinate> coords = AreaStringToCoordinates(area);
                            if (coords.Count == 0)
                            {
                                // Invalid coordinates provided
                            }
                            newArea = coords;
                            break;
                        }
                    case GeofenceType.Geofence:
                        {
                            List<List<Coordinate>> coords = AreaStringToMultiPolygon(area);
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
            ExpandoObject obj = BuildDefaultData();
            string data = Renderer.ParseTemplate("assignments", obj);
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
                IReadOnlyList<Instance> instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                IReadOnlyList<Device> devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                dynamic obj = BuildDefaultData();
                obj.instances = instances.Select(x => new { name = x.Name, selected = false, selected_source = false });
                obj.devices = devices.Select(x => new { uuid = x.Uuid, selected = false });
                obj.nothing_selected = true;
                obj.nothing_selected_source = true;
                dynamic data = Renderer.ParseTemplate("assignment-add", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                string uuid = Request.Form["device"].ToString();
                string sourceInstance = Request.Form["source_instance"].ToString();
                string destinationInstance = Request.Form["instance"].ToString();
                string time = Request.Form["time"].ToString();
                string date = Request.Form["date"].ToString();
                bool createOnComplete = Request.Form["oncomplete"].ToString() == "on";
                bool enabled = Request.Form["enabled"].ToString() == "on";

                IReadOnlyList<Instance> instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                IReadOnlyList<Device> devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
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

                uint totalTime = 0u;
                if (!string.IsNullOrEmpty(time))
                {
                    string[] split = time.Split(':');
                    if (split.Length == 3)
                    {
                        int hours = int.Parse(split[0]);
                        int minutes = int.Parse(split[1]);
                        int seconds = int.Parse(split[2]);
                        int newTime = (hours * 3600) + (minutes * 60) + seconds;
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
                    Assignment assignment = new Assignment
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
                    Assignment oncompleteAssignment = new Assignment
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
                Assignment assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
                if (assignment == null)
                {
                    // Failed to get assignment by id, does assignment exist?
                }

                IReadOnlyList<Device> devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                IReadOnlyList<Instance> instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                if (devices == null || instances == null)
                {
                    // Failed to get devices or instances from database
                }

                string formattedTime = assignment.Time == 0 ? "" : $"{assignment.Time / 3600:00}:{assignment.Time % 3600 / 60:00}:{assignment.Time % 3600 % 60:00}";
                dynamic obj = BuildDefaultData();
                obj.id = id;
                obj.date = assignment.Date;
                obj.time = formattedTime;
                obj.enabled = assignment.Enabled ? "checked" : "";
                obj.instances = instances.Select(x => new { name = x.Name, selected = x.Name == assignment.InstanceName, selected_source = x.Name == assignment.SourceInstanceName });
                obj.devices = devices.Select(x => new { uuid = x.Uuid, selected = x.Uuid == assignment.DeviceUuid });
                dynamic data = Renderer.ParseTemplate("assignment-edit", obj);
                return new ContentResult
                {
                    Content = data,
                    ContentType = "text/html",
                    StatusCode = 200,
                };
            }
            else
            {
                string uuid = Request.Form["device"].ToString();
                string sourceInstance = Request.Form["source_instance"].ToString();
                string destinationInstance = Request.Form["instance"].ToString();
                string time = Request.Form["time"].ToString();
                string date = Request.Form["date"].ToString();
                bool createOnComplete = Request.Form["oncomplete"].ToString() == "on";
                bool enabled = Request.Form["enabled"].ToString() == "on";

                IReadOnlyList<Instance> instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
                IReadOnlyList<Device> devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
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

                uint totalTime = 0u;
                if (!string.IsNullOrEmpty(time))
                {
                    string[] split = time.Split(':');
                    if (split.Length == 3)
                    {
                        int hours = int.Parse(split[0]);
                        int minutes = int.Parse(split[1]);
                        int seconds = int.Parse(split[2]);
                        int newTime = (hours * 3600) + (minutes * 60) + seconds;
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

                Assignment assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
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
            Assignment assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
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
            Assignment assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
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

        #region Accounts

        [HttpGet("/dashboard/accounts")]
        public async Task<IActionResult> GetAccounts()
        {
            AccountRepository accountsRepository = new AccountRepository(_context);
            dynamic stats = await accountsRepository.GetStatsAsync().ConfigureAwait(false);
            dynamic obj = BuildDefaultData();
            obj.stats = stats;
            dynamic data = Renderer.ParseTemplate("accounts", obj);
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
                dynamic data = Renderer.ParseTemplate("accounts-add", obj);
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
                ushort level = ushort.Parse(Request.Form["level"].ToString());
                string accountsToAdd = Request.Form["accounts"].ToString();
                string[] rows = accountsToAdd.Replace(";", ",")
                                        .Replace(":", ",")
                                        .Split('\n');
                List<Account> list = new List<Account>();
                foreach (string row in rows)
                {
                    string[] split = row.Split(',');
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
                ExpandoObject obj = BuildDefaultData();
                string data = Renderer.ParseTemplate("clearquests", obj);
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

        [HttpGet("/dashboard/settings")]
        public IActionResult GetSettings()
        {
            ExpandoObject obj = BuildDefaultData();
            string data = Renderer.ParseTemplate("settings", obj);
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
            string coords = string.Empty;
            NumberFormatInfo nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (dynamic coord in area.EnumerateArray())
            {
                dynamic latitude = double.Parse(Convert.ToString(coord.GetProperty("lat")), nfi);
                dynamic longitude = double.Parse(Convert.ToString(coord.GetProperty("lon")), nfi);
                coords += $"{latitude},{longitude}\n";
            }
            return coords;
        }

        private static string MultiPolygonToAreaString(dynamic area)
        {
            int index = 1;
            string coords = string.Empty;
            NumberFormatInfo nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (dynamic fence in area.EnumerateArray())
            {
                coords += $"[Geofence {index}]\n";
                foreach (dynamic coord in fence.EnumerateArray())
                {
                    dynamic latitude = double.Parse(Convert.ToString(coord.GetProperty("lat")), nfi);
                    dynamic longitude = double.Parse(Convert.ToString(coord.GetProperty("lon")), nfi);
                    coords += $"{latitude},{longitude}\n";
                }
                index++;
            }
            return coords;
        }

        private static List<Coordinate> AreaStringToCoordinates(string area)
        {
            string[] rows = area.Split('\n');
            List<Coordinate> coords = new List<Coordinate>();
            NumberFormatInfo nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (string row in rows)
            {
                string[] split = row.Split(',');
                if (split.Length != 2)
                {
                    continue;
                }

                double latitude = double.Parse(split[0].Trim('\n'), nfi);
                double longitude = double.Parse(split[1].Trim('\n'), nfi);
                coords.Add(new Coordinate(latitude, longitude));
            }
            return coords;
        }

        private static List<List<Coordinate>> AreaStringToMultiPolygon(string area)
        {
            string[] rows = area.Split('\n');
            int index = 0;
            List<List<Coordinate>> coords = new List<List<Coordinate>> { new List<Coordinate>() };
            NumberFormatInfo nfi = new CultureInfo("en-US").NumberFormat;
            nfi.NumberDecimalSeparator = ".";
            foreach (string row in rows)
            {
                string[] split = row.Split(',');
                if (split.Length == 2)
                {
                    double latitude = double.Parse(split[0].Trim('\0'), nfi);
                    double longitude = double.Parse(split[1].Trim('\0'), nfi);
                    coords[index].Add(new Coordinate(latitude, longitude));
                }
                else if (row.Contains("[") && row.Contains("]") && coords.Count > index && coords[index].Count > 0)
                {                   
                    coords.Add(new List<Coordinate>());
                    index++;
                }
            }
            return coords;
        }

        private static IActionResult BuildErrorResponse(string template, string message)
        {
            dynamic obj = BuildDefaultData();
            obj.show_error = true;
            obj.error = message;
            dynamic data = Renderer.ParseTemplate(template, obj);
            return new ContentResult
            {
                Content = data,
                ContentType = "text/html",
                StatusCode = 200,
            };
        }

        #endregion
    }
}