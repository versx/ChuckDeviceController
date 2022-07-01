namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Controller]
    public class DeviceController : Controller
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly DeviceControllerContext _context;
        private readonly IJobControllerService _jobControllerService;

        public DeviceController(
            ILogger<DeviceController> logger,
            DeviceControllerContext context,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _context = context;
            _jobControllerService = jobControllerService;
        }

        // GET: DeviceController
        public ActionResult Index()
        {
            var devices = _context.Devices.ToList();
            var model = new ViewModelsModel<Device>
            {
                Items = devices,
            };
            return View(model);
        }

        // GET: DeviceController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                // Failed to retrieve device from database, does it exist?
                ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                return View();
            }
            return View(device);
        }

        // GET: DeviceController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DeviceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Create device
                // TODO: _jobControllerService.AddDevice(device);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Device", $"Unknown error occurred while creating new device.");
                return View();
            }
        }

        // GET: DeviceController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                // Failed to retrieve device from database, does it exist?
                ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                return View();
            }
            return View(device);
        }

        // POST: DeviceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                {
                    // Failed to retrieve device from database, does it exist?
                    ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                    return View();
                }

                // TODO: Edit Device
                _jobControllerService.ReloadDevice(device, id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Device", $"Unknown error occurred while editing device '{id}'.");
                return View();
            }
        }

        // GET: DeviceController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
            {
                // Failed to retrieve device from database, does it exist?
                ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                return View();
            }
            return View(device);
        }

        // POST: DeviceController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                {
                    // Failed to retrieve device from database, does it exist?
                    ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                    return View();
                }

                // Delete device from database
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();

                _jobControllerService.RemoveDevice(id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Device", $"Unknown error occurred while deleting device '{id}'.");
                return View();
            }
        }
    }
}
