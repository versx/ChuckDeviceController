namespace ChuckDeviceConfigurator.Controllers
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Data;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Plugin;
    using ChuckDeviceController.Plugin.EventBus;
    using ChuckDeviceController.Plugin.EventBus.Events;
    using ChuckDeviceController.Plugin.EventBus.Observer;
    using ChuckDeviceController.PluginManager;

    [Authorize(Roles = RoleConsts.DefaultRole)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger =
            new Logger<HomeController>(LoggerFactory.Create(x => x.AddConsole()));
        private readonly ControllerDbContext _deviceContext;
        private readonly MapDbContext _mapContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUiHost _uiHost;
        private readonly IEventAggregatorHost _eventAggregatorHost;

        public HomeController(
            ControllerDbContext deviceContext,
            MapDbContext mapContext,
            UserManager<ApplicationUser> userManager,
            IUiHost uiHost,
            IEventAggregatorHost eventAggregatorHost)
        {
            _deviceContext = deviceContext;
            _mapContext = mapContext;
            _userManager = userManager;
            _uiHost = uiHost;
            _eventAggregatorHost = eventAggregatorHost;
        }

        public IActionResult Index()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var uptime = Strings.Uptime;
            var uptimeLocal = uptime.ToLocalTime();
            var model = new DashboardViewModel
            {
                Accounts = (ulong)_deviceContext.Accounts.LongCount(),
                Assignments = (ulong)_deviceContext.Assignments.LongCount(),
                AssignmentGroups = (ulong)_deviceContext.AssignmentGroups.LongCount(),
                Devices = (ulong)_deviceContext.Devices.LongCount(),
                DeviceGroups = (ulong)_deviceContext.DeviceGroups.LongCount(),
                Geofences = (ulong)_deviceContext.Geofences.LongCount(),
                Instances = (ulong)_deviceContext.Instances.LongCount(),
                IvLists = (ulong)_deviceContext.IvLists.LongCount(),
                Plugins = (ulong)PluginManager.Instance.Plugins.LongCount(),
                Webhooks = (ulong)_deviceContext.Webhooks.LongCount(),
                Users = (ulong)_userManager.Users.LongCount(),

                Gyms = (ulong)_mapContext.Gyms.LongCount(),
                GymDefenders = (ulong)_mapContext.GymDefenders.LongCount(),
                GymTrainers = (ulong)_mapContext.GymTrainers.LongCount(),
                Raids = (ulong)_mapContext.Gyms.LongCount(gym => gym.RaidEndTimestamp >= now),
                Incidents = (ulong)_mapContext.Incidents.LongCount(),
                Pokemon = (ulong)_mapContext.Pokemon.LongCount(),
                Pokestops = (ulong)_mapContext.Pokestops.LongCount(),
                Lures = (ulong)_mapContext.Pokestops.LongCount(pokestop => pokestop.LureExpireTimestamp >= now),
                Quests = (ulong)_mapContext.Pokestops.LongCount(pokestop => pokestop.QuestType != null || pokestop.AlternativeQuestType != null),
                Cells = (ulong)_mapContext.Cells.LongCount(),
                Spawnpoints = (ulong)_mapContext.Spawnpoints.LongCount(),
                Weather = (ulong)_mapContext.Weather.LongCount(),

                PluginDashboardStats = _uiHost.DashboardStatsItems,

                Uptime = uptime.ToTotalSeconds().ToReadableString(includeAgoText: false),
                Started = $"{uptimeLocal.ToLongDateString()} {uptimeLocal.ToLongTimeString()}",
            };

            //_eventAggregatorHost.Subscribe(new PluginObserver());
            //_eventAggregatorHost.Publish(new PluginEvent("Test from HomeController"));

            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Privacy(string? handler = null)
        {
            var trackingConsentFeature = HttpContext.Features.Get<ITrackingConsentFeature>();
            trackingConsentFeature?.WithdrawConsent();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(model);
        }
    }
}