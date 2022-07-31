namespace ChuckDeviceConfigurator.Controllers
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Data;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;

    [Authorize(Roles = RoleConsts.DefaultRole)]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DeviceControllerContext _deviceContext;
        private readonly MapDataContext _mapContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            DeviceControllerContext deviceContext,
            MapDataContext mapContext,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _deviceContext = deviceContext;
            _mapContext = mapContext;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                Accounts = (uint)_deviceContext.Accounts.Count(),
                Assignments = (uint)_deviceContext.Assignments.Count(),
                AssignmentGroups = (uint)_deviceContext.AssignmentGroups.Count(),
                Devices = (uint)_deviceContext.Devices.Count(),
                DeviceGroups = (uint)_deviceContext.DeviceGroups.Count(),
                Geofences = (uint)_deviceContext.Geofences.Count(),
                Instances = (uint)_deviceContext.Instances.Count(),
                IvLists = (uint)_deviceContext.IvLists.Count(),
                Webhooks = (uint)_deviceContext.Webhooks.Count(),
                Users = (uint)_userManager.Users.Count(),

                Gyms = (uint)_mapContext.Gyms.Count(),
                GymDefenders = (uint)_mapContext.GymDefenders.Count(),
                GymTrainers = (uint)_mapContext.GymTrainers.Count(),
                Incidents = (uint)_mapContext.Incidents.Count(),
                Pokemon = (uint)_mapContext.Pokemon.Count(),
                Pokestops = (uint)_mapContext.Pokestops.Count(),
                Cells = (uint)_mapContext.Cells.Count(),
                Spawnpoints = (uint)_mapContext.Spawnpoints.Count(),
                Weather = (uint)_mapContext.Weather.Count(),
            };
            return View(model);
        }

        public IActionResult About()
        {
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