namespace ChuckDeviceConfigurator.Controllers
{
    using System.Diagnostics;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Data;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;

    // TODO: [Authorize(Roles = nameof(Roles.Registered))]
    [Authorize(Roles = $"{nameof(Roles.Registered)}, {nameof(Roles.SuperAdmin)}, {nameof(Roles.Admin)}")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DeviceControllerContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            DeviceControllerContext context)
        {
            _logger = logger;
            _context = context;
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
                Users = 0, //(uint)_context.Users.Count(),
            };
            return View(model);
        }

        public IActionResult Privacy()
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