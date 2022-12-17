namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;

    [Controller]
    [Authorize(Roles = RoleConsts.DeviceGroupsRole)]
    public class DeviceGroupController : Controller
    {
        private readonly ILogger<DeviceGroupController> _logger;
        private readonly IUnitOfWork _uow;

        public DeviceGroupController(
            ILogger<DeviceGroupController> logger,
            IUnitOfWork uow)
        {
            _logger = logger;
            _uow = uow;
        }

        // GET: DeviceGroupController
        public async Task<ActionResult> Index()
        {
            var deviceGroups = await _uow.DeviceGroups.FindAllAsync();
            var model = new ViewModelsModel<DeviceGroup>
            {
                Items = deviceGroups.ToList(),
            };
            return View(model);
        }

        // GET: DeviceGroupController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var deviceGroup = await _uow.DeviceGroups.FindByIdAsync(id);
            if (deviceGroup == null)
            {
                // Failed to retrieve device group from database, does it exist?
                ModelState.AddModelError("DeviceGroup", $"DeviceGroup does not exist with id '{id}'.");
                return View();
            }

            // Get list of devices for device group from database and set their online/offline status
            var devices = await _uow.Devices.FindAsync(device => deviceGroup.DeviceUuids.Contains(device.Uuid));
            var model = new DeviceGroupDetailsViewModel
            {
                Name = deviceGroup.Name,
                DeviceUuids = deviceGroup.DeviceUuids,
                Devices = devices.ToList(),
            };
            ViewBag.Devices = devices;
            return View(model);
        }

        // GET: DeviceGroupController/Create
        public async Task<ActionResult> Create()
        {
            var devices = await _uow.Devices.FindAllAsync();
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
                var devices = collection["DeviceUuids"].ToList();

                if (_uow.DeviceGroups.Any(deviceGroup => deviceGroup.Name == name))
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
                    DeviceUuids = devices,
                };

                // Add device group to database
                await _uow.DeviceGroups.AddAsync(deviceGroup);
                await _uow.CommitAsync();

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
            var deviceGroup = await _uow.DeviceGroups.FindByIdAsync(id);
            if (deviceGroup == null)
            {
                // Failed to retrieve device group from database, does it exist?
                ModelState.AddModelError("DeviceGroup", $"Device group does not exist with id '{id}'.");
                return View();
            }

            var devices = await _uow.Devices.FindAllAsync();
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
                var deviceGroup = await _uow.DeviceGroups.FindByIdAsync(id);
                if (deviceGroup == null)
                {
                    // Failed to retrieve device group from database, does it exist?
                    ModelState.AddModelError("DeviceGroup", $"Device group does not exist with id '{id}'.");
                    return View();
                }

                var name = Convert.ToString(collection["Name"]);
                var devices = collection["DeviceUuids"].ToList();

                if (_uow.DeviceGroups.Any(deviceGroup => deviceGroup.Name == name && deviceGroup.Name != id))
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
                deviceGroup.DeviceUuids = devices;

                // Update device group to database
                await _uow.DeviceGroups.UpdateAsync(deviceGroup);
                await _uow.CommitAsync();

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
            var deviceGroup = await _uow.DeviceGroups.FindByIdAsync(id);
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
                var deviceGroup = await _uow.DeviceGroups.FindByIdAsync(id);
                if (deviceGroup == null)
                {
                    // Failed to retrieve device group from database, does it exist?
                    ModelState.AddModelError("DeviceGroup", $"Device group does not exist with id '{id}'.");
                    return View();
                }

                // Delete device group from database
                await _uow.DeviceGroups.RemoveAsync(deviceGroup);
                await _uow.CommitAsync();

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