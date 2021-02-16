namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.JobControllers;
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
            obj.devices_count = _context.Devices.Count();
            obj.instances_count = _context.Instances.Count();
            obj.assignments_count = _context.Assignments.Count();
            obj.accounts_count = _context.Accounts.Count();
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
                }
                var instance = await _instanceRepository.GetByIdAsync(instanceName).ConfigureAwait(false);
                if (instance == null)
                {
                    // Failed to get instance by name
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
        public async Task<IActionResult> CreateInstance()
        {
            if (Request.Method == "GET")
            {
                dynamic obj = BuildDefaultData();
                obj.min_level = 0;
                obj.max_level = 30;
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
                var area = Request.Form["area"].ToString();
                var minLevel = ushort.Parse(Request.Form["min_level"]);
                var maxLevel = ushort.Parse(Request.Form["max_level"]);
                var timezoneOffset = Request.Form.ContainsKey("timezone_offset")
                    ? int.Parse(Request.Form["timezone_offset"].ToString() ?? "0")
                    : 0;
                // TODO: Check if == '*' and generate list of ids
                var pokemonIds = new List<uint>();
                ushort ivQueueLimit = 100;
                ushort spinLimit = 3500;
                if (type == InstanceType.PokemonIV)
                {
                    if (Request.Form.ContainsKey("pokemon_ids") && Request.Form["pokemon_ids"].Count > 0)
                    {
                        pokemonIds = Request.Form["pokemon_ids"].ToString()?.Split('\n')?.Select(x => uint.Parse(x)).ToList();
                    }
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
                else if (type == InstanceType.Bootstrap)
                {
                    // TODO: Bootstrap circle size
                }
                //var scatterPokemonIds = Request.Form["scatter_pokemon_ids"];
                //var accountGroup = Request.Form["account_group"];
                //var isEvent = Request.Form["is_event"];
                if (minLevel > maxLevel || minLevel == 0 || minLevel > 40 || maxLevel == 0 || maxLevel > 40)
                {
                    // Invalid levels
                }

                if (await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false) != null)
                {
                    // Instance already exists
                }

                dynamic newArea = null;
                switch (type)
                {
                    case InstanceType.CirclePokemon:
                    case InstanceType.CircleRaid:
                    case InstanceType.SmartCircleRaid:
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
                    case InstanceType.AutoQuest:
                    case InstanceType.PokemonIV:
                    case InstanceType.Bootstrap:
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

                var instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (instance != null)
                {
                    // Instance already exists
                    return null;
                }

                instance = new Instance
                {
                    Name = name,
                    Type = type,
                    Data = new InstanceData
                    {
                        IsEvent = false,
                        Area = newArea,
                        IVQueueLimit = ivQueueLimit,
                        SpinLimit = spinLimit,
                        MinimumLevel = minLevel,
                        MaximumLevel = maxLevel,
                        PokemonIds = pokemonIds,
                        TimezoneOffset = timezoneOffset,
                    }
                };
                await _instanceRepository.AddAsync(instance).ConfigureAwait(false);
                InstanceController.Instance.AddInstance(instance);
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
                }
                var minLevel = instance.Data.MinimumLevel;
                var maxLevel = instance.Data.MaximumLevel;
                dynamic obj = BuildDefaultData();
                obj.name = name;
                obj.old_name = name;
                obj.min_level = minLevel;
                obj.max_level = maxLevel;
                obj.circle_pokemon_selected = instance.Type == InstanceType.CirclePokemon;
                obj.circle_raid_selected = instance.Type == InstanceType.CircleRaid;
                obj.pokemon_iv_selected = instance.Type == InstanceType.PokemonIV;
                obj.auto_quest_selected = instance.Type == InstanceType.AutoQuest;
                obj.bootstrap_selected = instance.Type == InstanceType.Bootstrap;
                //switch (instance.Type)
                //{
                //    case InstanceType.PokemonIV:
                        obj.pokemon_ids = instance.Data.PokemonIds == null ? null : string.Join("\n", instance.Data.PokemonIds);
                        obj.iv_queue_limit = instance.Data.IVQueueLimit > 0 ? instance.Data.IVQueueLimit : 100;
                //        break;
                //    case InstanceType.AutoQuest:
                        obj.timezone_offset = instance.Data.TimezoneOffset;
                        obj.spin_limit = instance.Data.SpinLimit > 0 ? instance.Data.SpinLimit : 3500;
                //        break;
                //}
                if (instance.Type == InstanceType.CirclePokemon ||
                    instance.Type == InstanceType.CircleRaid ||
                    instance.Type == InstanceType.SmartCircleRaid)
                {
                    var coords = string.Empty;
                    var coordsArray = instance.Data.Area;
                    foreach (var coord in coordsArray.EnumerateArray())
                    {
                        coords += $"{coord.GetProperty("lat").GetDouble()},{coord.GetProperty("lon").GetDouble()}\n";
                    }
                    obj.area = coords;
                }
                else if (instance.Type == InstanceType.AutoQuest ||
                         instance.Type == InstanceType.PokemonIV ||
                         instance.Type == InstanceType.Bootstrap)
                {
                    var coords = string.Empty;
                    var coordsArray = instance.Data.Area;
                    var index = 1;
                    foreach (var geofence in coordsArray.EnumerateArray())
                    {
                        coords += $"[Geofence {index}]\n";
                        foreach (var coord in geofence.EnumerateArray())
                        {
                            coords += $"{coord.GetProperty("lat").GetDouble()},{coord.GetProperty("lon").GetDouble()}\n";
                        }
                        index++;
                    }
                    obj.area = coords;
                }
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
                var area = Request.Form["area"].ToString();
                var minLevel = ushort.Parse(Request.Form["min_level"]);
                var maxLevel = ushort.Parse(Request.Form["max_level"]);
                var timezoneOffset = int.Parse(Request.Form["timezone_offset"].ToString() ?? "0");
                // TODO: Check if == '*' and generate list of ids
                var pokemonIds = Request.Form["pokemon_ids"].ToString().Split('\n').Select(uint.Parse).ToList();
                //var scatterPokemonIds = Request.Form["scatter_pokemon_ids"];
                var ivQueueLimit = ushort.Parse(Request.Form["iv_queue_limit"]);
                var spinLimit = ushort.Parse(Request.Form["spin_limit"]);
                //var accountGroup = Request.Form["account_group"];
                //var isEvent = Request.Form["is_event"];
                if (minLevel > maxLevel || minLevel == 0 || minLevel > 40 || maxLevel == 0 || maxLevel > 40)
                {
                    // Invalid levels
                }

                dynamic newArea = null;
                switch (type)
                {
                    case InstanceType.CirclePokemon:
                    case InstanceType.CircleRaid:
                    case InstanceType.SmartCircleRaid:
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
                    case InstanceType.AutoQuest:
                    case InstanceType.PokemonIV:
                    case InstanceType.Bootstrap:
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

                var instance = await _instanceRepository.GetByIdAsync(name).ConfigureAwait(false);
                if (instance == null)
                {
                    // Instance does not exist, create?
                }

                instance.Name = newName;
                instance.Type = type;
                instance.Data = new InstanceData
                {
                    IsEvent = false,
                    Area = newArea,
                    IVQueueLimit = ivQueueLimit,
                    SpinLimit = spinLimit,
                    MinimumLevel = minLevel,
                    MaximumLevel = maxLevel,
                    PokemonIds = pokemonIds,
                    TimezoneOffset = timezoneOffset,
                };
                await _instanceRepository.UpdateAsync(instance).ConfigureAwait(false);
                InstanceController.Instance.ReloadInstance(instance, name);
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
                }

                if (!devices.Any(x => x.Uuid == uuid))
                {
                    // Device does not exist
                }

                if (!instances.Any(x => string.Compare(x.Name, destinationInstance, true) == 0))
                {
                    // Instance does not exist
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
                        return null;
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
                }

                if (!devices.Any(x => x.Uuid == uuid))
                {
                    // Device does not exist
                }

                if (!instances.Any(x => string.Compare(x.Name, destinationInstance, true) == 0))
                {
                    // Instance does not exist
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
                        return null;
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
                }

                var assignment = await _assignmentRepository.GetByIdAsync(id).ConfigureAwait(false);
                if (assignment == null)
                {
                    // Failed to get assignment by id
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
                // Failed to delete assignment by id, does it exist?
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
            var accountsRepository = new AccountRepository(_context);
            var stats = await accountsRepository.GetStatsAsync().ConfigureAwait(false);
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

        [HttpGet("/dashboard/settings")]
        public IActionResult GetSettings()
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

        #endregion

        #region Helper Methods

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

        #endregion
    }
}
