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
        private readonly DeviceControllerContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            DeviceControllerContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var model = new DashboardViewModel
            {
                Accounts = (uint)_context.Accounts.Count(),
                Assignments = (uint)_context.Assignments.Count(),
                Devices = (uint)_context.Devices.Count(),
                Geofences = (uint)_context.Geofences.Count(),
                Instances = (uint)_context.Instances.Count(),
                IvLists = (uint)_context.IvLists.Count(),
                Webhooks = (uint)_context.Webhooks.Count(),
                Users = (uint)_userManager.Users.Count(),
            };
            return View(model);
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