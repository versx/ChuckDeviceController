namespace ChuckDeviceConfigurator.Controllers;

using System.Diagnostics;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.Data;
using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Plugin;
using ChuckDeviceController.Plugin.EventBus;
using ChuckDeviceController.PluginManager;

[Authorize(Roles = RoleConsts.DefaultRole)]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDapperUnitOfWork _uow;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUiHost _uiHost;
    private readonly IEventAggregatorHost _eventAggregatorHost;

    public HomeController(
        ILogger<HomeController> logger,
        IDapperUnitOfWork uow,
        UserManager<ApplicationUser> userManager,
        IUiHost uiHost,
        IEventAggregatorHost eventAggregatorHost)
    {
        _logger = logger;
        _uow = uow;
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
            Accounts = (ulong)_uow.Accounts.Count(),
            Assignments = (ulong)_uow.Assignments.Count(),
            AssignmentGroups = (ulong)_uow.AssignmentGroups.Count(),
            Devices = (ulong)_uow.Devices.Count(),
            DeviceGroups = (ulong)_uow.DeviceGroups.Count(),
            Geofences = (ulong)_uow.Geofences.Count(),
            Instances = (ulong)_uow.Instances.Count(),
            IvLists = (ulong)_uow.IvLists.Count(),
            Plugins = (ulong)PluginManager.Instance.Plugins.Count,
            Webhooks = (ulong)_uow.Webhooks.Count(),
            Users = (ulong)_userManager.Users.Count(),

            Cells = (ulong)_uow.Cells.Count(),
            Gyms = (ulong)_uow.Gyms.Count(),
            GymDefenders = (ulong)_uow.GymDefenders.Count(),
            GymTrainers = (ulong)_uow.GymTrainers.Count(),
            Raids = (ulong)_uow.Gyms.Count(gym => gym.RaidEndTimestamp >= now),
            Pokestops = (ulong)_uow.Pokestops.Count(),
            Lures = (ulong)_uow.Pokestops.Count(pokestop => pokestop.LureExpireTimestamp >= now),
            Incidents = (ulong)_uow.Incidents.Count(),
            Quests = (ulong)_uow.Pokestops.Count(pokestop => pokestop.QuestType != null || pokestop.AlternativeQuestType != null),
            Pokemon = (ulong)_uow.Pokemon.Count(),
            Spawnpoints = (ulong)_uow.Spawnpoints.Count(),
            Weather = (ulong)_uow.Weather.Count(),

            PluginDashboardStats = _uiHost.DashboardStatsItems,
            PluginDashboardTiles = _uiHost.DashboardTiles,

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