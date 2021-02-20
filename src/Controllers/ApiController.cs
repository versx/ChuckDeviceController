namespace ChuckDeviceController.Controllers
{
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.JobControllers;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [ApiController]
    [Route("/api")]
    public class ApiController : ControllerBase
    {
        #region Variables

        // Database repository wrappers
        private readonly DeviceRepository _deviceRepository;
        private readonly InstanceRepository _instanceRepository;
        private readonly AssignmentRepository _assignmentRepository;
        private readonly GeofenceRepository _geofenceRepository;

        // Dependency injection variables
        private readonly DeviceControllerContext _context;
        private readonly ILogger<DeviceController> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Api Controller
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public ApiController(DeviceControllerContext context, ILogger<DeviceController> logger)
        {
            _context = context;
            _logger = logger;

            _deviceRepository = new DeviceRepository(_context);
            _instanceRepository = new InstanceRepository(_context);
            _assignmentRepository = new AssignmentRepository(_context);
            _geofenceRepository = new GeofenceRepository(_context);
        }

        #endregion

        #region Routes

        /// <summary>
        /// Get all devices
        /// </summary>
        /// <returns>Returns a list of all device objects</returns>
        [
            HttpPost("/api/devices"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetDevices()
        {
            // TODO: Use formatted in query
            IReadOnlyList<Device> devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
            List<dynamic> list = new List<dynamic>();
            foreach (Device device in devices)
            {
                const ulong delta = 15 * 60;
                ulong diff = DateTime.UtcNow.ToTotalSeconds() - delta;
                int isOnline = device.LastSeen > diff ? 0 : 1;
                DateTime lastSeenDate = device.LastSeen.Value.FromUnix();
                lastSeenDate = lastSeenDate.Add(TimeSpan.FromHours(Startup.Config.TimezoneOffset));
                list.Add(new
                {
                    is_online = isOnline,
                    uuid = device.Uuid,
                    account_username = device.AccountUsername,
                    instance_name = device.InstanceName,
                    last_host = device.LastHost,
                    last_seen = new
                    {
                        formatted = lastSeenDate.ToString("MM/dd/yyyy hh:mm:ss"),
                        sort = device.LastSeen,
                    },
                    image = new
                    {
                        type = "device",
                        status = isOnline,
                    },
                    buttons = $"<a href='/dashboard/device/assign/{Uri.EscapeDataString(device.Uuid)}' role='button' class='btn btn-sm btn-primary'>Assign Instance</a>",
                });
            }
            return new { data = new { devices = list } };
        }

        /// <summary>
        /// Get all instances
        /// </summary>
        /// <returns>Returns a list of all instance objects</returns>
        [
            HttpPost("/api/instances"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetInstances()
        {
            ulong now = DateTime.UtcNow.ToTotalSeconds();
            IReadOnlyList<Instance> instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
            IReadOnlyList<Device> devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
            List<dynamic> list = new List<dynamic>();
            const ulong delta = 15 * 60;
            foreach (Instance instance in instances)
            {
                IEnumerable<Device> instanceDevices = devices.Where(device => string.Compare(device.InstanceName, instance.Name, true) == 0);
                int totalCount = instanceDevices.Count();
                int onlineCount = instanceDevices.Count(device => device.LastSeen >= now - delta);
                int offlineCount = instanceDevices.Count(device => device.LastSeen < now - delta);
                list.Add(new
                {
                    name = instance.Name,
                    type = FormatInstanceType(instance.Type),
                    count = totalCount == 0 ? "0" : $"{onlineCount}/{offlineCount} ({totalCount})",
                    geofence = instance.Geofence,
                    status = await InstanceController.Instance.GetInstanceStatus(instance).ConfigureAwait(false),
                    buttons = $"<a href='/dashboard/instance/edit/{Uri.EscapeDataString(instance.Name)}' role='button' class='btn btn-sm btn-primary'>Edit Instance</a>",
                });
            }
            return new { data = new { instances = list } };
        }

        [
            HttpPost("/api/geofences"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetGeofences()
        {
            IReadOnlyList<Geofence> geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
            List<dynamic> list = new List<dynamic>();
            foreach (Geofence geofence in geofences)
            {
                list.Add(new
                {
                    name = geofence.Name,
                    type = geofence.Type.ToString(),
                    count = geofence.Data.Area.GetArrayLength().ToString("N0"),
                    buttons = $"<div class='btn-group' role='group'><a href='/dashboard/geofence/edit/{Uri.EscapeDataString(geofence.Name)}' role='button' class='btn btn-primary'>Edit</a>",
                });
            }
            return new { data = new { geofences = list } };
        }

        /// <summary>
        /// Get all device assignments
        /// </summary>
        /// <returns>Returns a list of all device assignment objects</returns>
        [
            HttpPost("/api/assignments"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetAssignments()
        {
            IReadOnlyList<Assignment> assignments = await _assignmentRepository.GetAllAsync().ConfigureAwait(false);
            List<dynamic> list = new List<dynamic>();
            foreach (Assignment assignment in assignments)
            {
                TimeSpan times = TimeSpan.FromSeconds(assignment.Time);
                string time = assignment.Time == 0
                    ? "On Complete"
                    : $"{times.Hours:00}:{times.Minutes:00}:{times.Seconds:00}";
                list.Add(new
                {
                    id = assignment.Id,
                    instance_name = assignment.InstanceName,
                    source_instance_name = assignment.SourceInstanceName,
                    device_uuid = assignment.DeviceUuid,
                    time = new
                    {
                        formatted = time,
                        timestamp = assignment.Time,
                    },
                    date = new
                    {
                        formatted = assignment.Date,
                        timestamp = assignment.Date,
                    },
                    enabled = assignment.Enabled ? "Yes" : "No",
                    buttons = $@"
<div class='btn-group' role='group'>
    <a href='/dashboard/assignment/start/{assignment.Id}' role='button' class='btn btn-sm btn-success'>Start</a>
    <a href='/dashboard/assignment/edit/{assignment.Id}' role='button' class='btn btn-sm btn-primary'>Edit</a>
    <a href='/dashboard/assignment/delete/{assignment.Id}' role='button' class='btn btn-sm btn-danger' onclick='return confirm(\'Are you sure you want to delete auto-assignments with id {assignment.Id}?\')'>Delete</a>
</div>
"
                });
            }
            return new { data = new { assignments = list } };
        }

        /// <summary>
        /// Get queue of IV instance
        /// </summary>
        /// <param name="name">Name of IV instance</param>
        /// <returns>Returns a list of Pokemon in specified IV instance queue</returns>
        [
            HttpPost("/api/ivqueue/{name}"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetIVQueue(string name)
        {
            List<Pokemon> queue = InstanceController.Instance.GetIVQueue(name);
            List<dynamic> list = new List<dynamic>();
            for (int i = 0; i < queue.Count; i++)
            {
                Pokemon pokemon = queue[i];
                list.Add(new
                {
                    id = i + 1,
                    pokemon_image = $"<img src='https://raw.githubusercontent.com/Mygod/PkmnHomeIcons/icons/icons/{pokemon.PokemonId}.png' style='height:50px; width:50px;'>",
                    pokemon_name = pokemon.PokemonId,
                    pokemon_id = pokemon.PokemonId,
                    form_id = pokemon.Form,
                    costume_id = pokemon.Costume,
                    pokemon_spawn_id = pokemon.Id,
                    location = $"<a href='https://maps.google.com/maps?q={pokemon.Latitude},{pokemon.Longitude}'>{Math.Round(pokemon.Latitude, 5)},{Math.Round(pokemon.Longitude, 5)}</a>",
                });
            }
            return await Task.FromResult(new
            {
                data = new
                {
                    instance_name = name,
                    ivqueue = list
                }
            }).ConfigureAwait(false);
        }

        #endregion

        private static string FormatInstanceType(InstanceType type)
        {
            return type switch
            {
                InstanceType.AutoQuest          => "Auto Quest",
                InstanceType.CirclePokemon      => "Circle Pokemon",
                InstanceType.CircleRaid         => "Circle Raid",
                InstanceType.SmartCircleRaid    => "Smart Raid",
                InstanceType.PokemonIV          => "Pokemon IV",
                InstanceType.Bootstrap          => "Bootstrap",
                _ => type.ToString(),
            };
        }
    }
}