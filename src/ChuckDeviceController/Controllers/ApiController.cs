namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using Chuck.Data.Contexts;
    using Chuck.Data.Entities;
    using Chuck.Data.Repositories;
    using Chuck.Extensions;
    using ChuckDeviceController.JobControllers;

    [ApiController]
    public class ApiController : ControllerBase
    {
        #region Variables

        // Database repository wrappers
        private readonly DeviceRepository _deviceRepository;
        private readonly InstanceRepository _instanceRepository;
        private readonly AssignmentRepository _assignmentRepository;
        private readonly GeofenceRepository _geofenceRepository;
        private readonly WebhookRepository _webhookRepository;
        private readonly DeviceGroupRepository _deviceGroupRepository;
        private readonly IVListRepository _ivListRepository;
        private readonly UserRepository _userRepository;

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

            // TODO: Maybe use the DbContextFactory instead of relying on the same one per repo
            _deviceRepository = new DeviceRepository(_context);
            _instanceRepository = new InstanceRepository(_context);
            _assignmentRepository = new AssignmentRepository(_context);
            _geofenceRepository = new GeofenceRepository(_context);
            _webhookRepository = new WebhookRepository(_context);
            _deviceGroupRepository = new DeviceGroupRepository(_context);
            _ivListRepository = new IVListRepository(_context);
            _userRepository = new UserRepository(_context);
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
            var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            foreach (var device in devices)
            {
                const ulong delta = 15 * 60;
                var diff = DateTime.UtcNow.ToTotalSeconds() - delta;
                var isOnline = device.LastSeen > diff ? 0 : 1;
                var lastSeenDate = device.LastSeen.Value.FromUnix().ToLocalTime();
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

        [
            HttpPost("/api/devicegroups"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetDeviceGroups()
        {
            var deviceGroups = await _deviceGroupRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            foreach (var deviceGroup in deviceGroups)
            {
                var devicesInGroup = await _deviceRepository.GetByIdsAsync(deviceGroup.Devices).ConfigureAwait(false);
                var instanceNames = devicesInGroup.Select(x => x.InstanceName).Distinct().ToList();
                list.Add(new
                {
                    name = deviceGroup.Name,
                    instances = string.Join(", ", instanceNames),
                    devices = deviceGroup.Devices.Count.ToString("N0"),
                    buttons = $@"
<div class='btn-group' role='group'>
    <a href='/dashboard/devicegroup/assign/{Uri.EscapeDataString(deviceGroup.Name)}' role='button' class='btn btn-sm btn-success'>Assign</a>
    <a href='/dashboard/devicegroup/edit/{Uri.EscapeDataString(deviceGroup.Name)}' role='button' class='btn btn-sm btn-primary'>Edit</a>
    <a href='/dashboard/devicegroup/delete/{Uri.EscapeDataString(deviceGroup.Name)}' role='button' class='btn btn-sm btn-danger' onclick='return confirm(""Are you sure you want to delete device group {deviceGroup.Name}?"")'>Delete</a>
</div>
"
                });
            }
            return new { data = new { devicegroups = list } };
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
            var now = DateTime.UtcNow.ToTotalSeconds();
            var instances = await _instanceRepository.GetAllAsync().ConfigureAwait(false);
            var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            const ulong delta = 15 * 60;
            foreach (var instance in instances)
            {
                var instanceDevices = devices.Where(device => string.Compare(device.InstanceName, instance.Name, true) == 0);
                var totalCount = instanceDevices.Count();
                var onlineCount = instanceDevices.Count(device => device.LastSeen >= now - delta);
                var offlineCount = instanceDevices.Count(device => device.LastSeen < now - delta);
                list.Add(new
                {
                    name = instance.Name,
                    type = FormatInstanceType(instance.Type),
                    level = $"{instance.MinimumLevel}-{instance.MaximumLevel}",
                    //count = totalCount == 0 ? "0" : $"{totalCount} ({onlineCount}/{offlineCount})",
                    count = totalCount == 0 ? "0" : $"{onlineCount}/{offlineCount}|{totalCount}",
                    geofences = string.Join(", ", instance.Geofences ?? new List<string>()),
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
            var geofences = await _geofenceRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            foreach (var geofence in geofences)
            {
                list.Add(new
                {
                    name = geofence.Name,
                    type = geofence.Type.ToString(),
                    count = geofence.Data.Area.GetArrayLength().ToString("N0"),
                    buttons = $"<a href='/dashboard/geofence/edit/{Uri.EscapeDataString(geofence.Name)}' role='button' class='btn btn-sm btn-primary'>Edit</a>",
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
            var assignments = await _assignmentRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            foreach (var assignment in assignments)
            {
                var times = TimeSpan.FromSeconds(assignment.Time);
                var time = assignment.Time == 0
                    ? "On Complete"
                    : $"{times.Hours:00}:{times.Minutes:00}:{times.Seconds:00}";
                list.Add(new
                {
                    id = assignment.Id,
                    instance_name = assignment.InstanceName,
                    source_instance_name = assignment.SourceInstanceName,
                    device_uuid = assignment.DeviceUuid,
                    device_group = assignment.DeviceGroupName,
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
    <a href='/dashboard/assignment/delete/{assignment.Id}' role='button' class='btn btn-sm btn-danger' onclick='return confirm(""Are you sure you want to delete auto-assignments with id {assignment.Id}?"")'>Delete</a>
</div>
"
                });
            }
            return new { data = new { assignments = list } };
        }

        /// <summary>
        /// Get all webhook endpoint entries
        /// </summary>
        /// <returns>Returns a list of all webhook endpoint objects</returns>
        [
            HttpPost("/api/webhooks"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetWebhooks()
        {
            var webhooks = await _webhookRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            foreach (var webhook in webhooks)
            {
                list.Add(new
                {
                    name = webhook.Name,
                    types = string.Join(", ", webhook.Types),
                    url = webhook.Url,
                    delay = webhook.Delay,
                    geofences = string.Join(", ", webhook.Geofences ?? new List<string>()),
                    enabled = webhook.Enabled ? "Yes" : "No",
                    buttons = $"<a href='/dashboard/webhook/edit/{Uri.EscapeDataString(webhook.Name)}' role='button' class='btn btn-sm btn-primary'>Edit</a>",
                });
            }
            return new { data = new { webhooks = list } };
        }

        [
            HttpPost("/api/users"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetUsers()
        {
            var users = await _userRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            foreach (var user in users)
            {
                list.Add(new
                {
                    name = user.Username,
                    permissions = user.Permissions,
                    status = "Test",
                    enabled = user.Enabled ? "Yes" : "No",
                    buttons = $"<a href='/dashboard/user/edit/{Uri.EscapeDataString(user.Username)}' role='button' class='btn btn-sm btn-primary'>Edit</a>",
                });
            }
            return new { data = new { users = list } };
        }

        [
            HttpPost("/api/ivlists"),
            Produces("application/json"),
        ]
        public async Task<dynamic> GetIVLists()
        {
            var ivLists = await _ivListRepository.GetAllAsync().ConfigureAwait(false);
            var list = new List<dynamic>();
            foreach (var ivList in ivLists)
            {
                list.Add(new
                {
                    name = ivList.Name,
                    pokemon_list_count = ivList.PokemonIDs.Count.ToString("N0"),
                    buttons = $"<a href='/dashboard/ivlist/edit/{Uri.EscapeDataString(ivList.Name)}' role='button' class='btn btn-sm btn-primary'>Edit</a>",
                    // TODO: Delete button
                });
            }
            return new { data = new { ivlists = list } };
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
            var queue = InstanceController.Instance.GetIVQueue(name);
            var list = new List<dynamic>();
            for (var i = 0; i < queue.Count; i++)
            {
                var pokemon = queue[i];
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
                InstanceType.FindTTH            => "Spawnpoint TTH Finder",
                _ => type.ToString(),
            };
        }
    }
}