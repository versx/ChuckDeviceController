namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;

    using ChuckDeviceConfigurator.Data;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Controller]
    [Authorize(Roles = $"{nameof(Roles.Devices)},{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)}")]
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
            var accountsInUse = _context.Devices.Select(device => device.AccountUsername)
                                                .ToList();
            var accounts = _context.Accounts.Where(account => !accountsInUse.Contains(account.Username))
                                            .ToList();
            var instances = _context.Instances.ToList();
            ViewBag.Instances = instances;
            ViewBag.Accounts = accounts;
            return View();
        }

        // POST: DeviceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var uuid = Convert.ToString(collection["Uuid"]);
                var instanceName = Convert.ToString(collection["InstanceName"]);
                var accountUsername = Convert.ToString(collection["AccountUsername"]);

                if (_context.Devices.Any(device => device.Uuid == uuid))
                {
                    // Device exists already by name
                    ModelState.AddModelError("Device", $"Device with UUID '{uuid}' already exists.");
                    return View();
                }

                var device = new Device
                {
                    Uuid = uuid,
                    InstanceName = instanceName,
                    AccountUsername = accountUsername,
                };

                // Add device to database
                await _context.Devices.AddAsync(device);
                await _context.SaveChangesAsync();

                _jobControllerService.AddDevice(device);

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

            var accountsInUse = _context.Devices.ToList()
                                                .Select(device => device.AccountUsername)
                                                .ToList();
            // Filter accounts that are not used by devices unless this device we are editing
            var accounts = _context.Accounts.Where(account => !accountsInUse.Contains(account.Username) || device.AccountUsername == account.Username)
                                            .ToList();
            var selectItemAccounts = accounts.Select(account =>
                new SelectListItem(account.Username, account.Username, string.Compare(account.Username, device.AccountUsername, true) == 0))
                .ToList();
            var selectedAccount = accounts.FirstOrDefault(account => string.Compare(account.Username, device.AccountUsername, true) == 0);
            var instances = _context.Instances.ToList();
            var instanceSelectItems = instances.Select(inst => new SelectListItem(inst.Name, inst.Name, inst.Name == device.InstanceName))
                                               .ToList();
            ViewBag.Instances = new SelectList(instances, "Name", "Name");
            ViewBag.Accounts = new SelectList(selectItemAccounts, "Username", "Username");
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

                var instanceName = Convert.ToString(collection["InstanceName"]);
                var accountUsername = Convert.ToString(collection["AccountUsername"]);
                // Check if device is not already assigned to instance before updating in database
                if (device.InstanceName != instanceName || device.AccountUsername != accountUsername)
                {
                    if (device.InstanceName != instanceName)
                    {
                        device.InstanceName = instanceName;
                    }
                    if (device.AccountUsername != accountUsername)
                    {
                        device.AccountUsername = accountUsername;

                        // TODO: If assigned account for device changes, force device to logout/switch accounts
                    }

                    _context.Devices.Update(device);
                    await _context.SaveChangesAsync();
                }

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
