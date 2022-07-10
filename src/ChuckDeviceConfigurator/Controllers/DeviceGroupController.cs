namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Controller]
    [Authorize(Roles = RoleConsts.DeviceGroupsRole)]
    public class DeviceGroupController : Controller
    {
        private readonly ILogger<DeviceGroupController> _logger;
        private readonly DeviceControllerContext _context;

        public DeviceGroupController(
            ILogger<DeviceGroupController> logger,
            DeviceControllerContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: DeviceGroupController
        public ActionResult Index()
        {
            var deviceGroups = _context.DeviceGroups.ToList();
            var model = new ViewModelsModel<DeviceGroup>
            {
                Items = deviceGroups,
            };
            return View(model);
        }

        // GET: DeviceGroupController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var deviceGroup = await _context.DeviceGroups.FindAsync(id);
            if (deviceGroup == null)
            {
                // Failed to retrieve device group from database, does it exist?
                ModelState.AddModelError("DeviceGroup", $"DeviceGroup does not exist with id '{id}'.");
                return View();
            }
            return View(deviceGroup);
        }

        // GET: DeviceGroupController/Create
        public ActionResult Create()
        {
            var devices = _context.Devices.ToList();
            ViewBag.Devices = devices;
            return View();
        }

        // POST: DeviceGroupController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Name"]);
                var devices = collection["Devices"].ToList();

                if (_context.DeviceGroups.Any(deviceGroup => deviceGroup.Name == name))
                {
                    // Device group exists already by name
                    ModelState.AddModelError("DeviceGroup", $"Device group with name '{name}' already exists.");
                    return View();
                }

                if (devices.Count == 0)
                {
                    // At least one device is required to create the device group
                    ModelState.AddModelError("DeviceGroup", $"At least one device is required to create the device group.");
                    return View();
                }

                var deviceGroup = new DeviceGroup
                {
                    Name = name,
                    Devices = devices,
                };

                // Add device group to database
                await _context.DeviceGroups.AddAsync(deviceGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("DeviceGroup", $"Unknown error occurred while creating new device group.");
                return View();
            }
        }

        // GET: DeviceGroupController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var deviceGroup = await _context.DeviceGroups.FindAsync(id);
            if (deviceGroup == null)
            {
                // Failed to retrieve device group from database, does it exist?
                ModelState.AddModelError("DeviceGroup", $"Device group does not exist with id '{id}'.");
                return View();
            }

            var devices = _context.Devices.ToList();
            ViewBag.Devices = devices;
            return View(deviceGroup);
        }

        // POST: DeviceGroupController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var deviceGroup = await _context.DeviceGroups.FindAsync(id);
                if (deviceGroup == null)
                {
                    // Failed to retrieve device group from database, does it exist?
                    ModelState.AddModelError("DeviceGroup", $"Device group does not exist with id '{id}'.");
                    return View();
                }

                var name = Convert.ToString(collection["Name"]);
                var devices = collection["Devices"].ToList();

                if (_context.DeviceGroups.Any(deviceGroup => deviceGroup.Name == name && deviceGroup.Name != id))
                {
                    // Device group exists already by name
                    ModelState.AddModelError("DeviceGroup", $"Device group with name '{name}' already exists.");
                    return View();
                }

                if (devices.Count == 0)
                {
                    // At least one device is required to create the device group
                    ModelState.AddModelError("DeviceGroup", $"At least one device is required to create the device group.");
                    return View();
                }

                deviceGroup.Name = name;
                deviceGroup.Devices = devices;

                // Update device group to database
                _context.DeviceGroups.Update(deviceGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("DeviceGroup", $"Unknown error occurred while editing device group '{id}'.");
                return View();
            }
        }

        // GET: DeviceGroupController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var deviceGroup = await _context.DeviceGroups.FindAsync(id);
            if (deviceGroup == null)
            {
                // Failed to retrieve device group from database, does it exist?
                ModelState.AddModelError("DeviceGroup", $"Device group does not exist with id '{id}'.");
                return View();
            }
            return View(deviceGroup);
        }

        // POST: DeviceGroupController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var deviceGroup = await _context.DeviceGroups.FindAsync(id);
                if (deviceGroup == null)
                {
                    // Failed to retrieve device group from database, does it exist?
                    ModelState.AddModelError("DeviceGroup", $"Device group does not exist with id '{id}'.");
                    return View();
                }

                // Delete device group from database
                _context.DeviceGroups.Remove(deviceGroup);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("DeviceGroup", $"Unknown error occurred while deleting device group '{id}'.");
                return View();
            }
        }
    }
}