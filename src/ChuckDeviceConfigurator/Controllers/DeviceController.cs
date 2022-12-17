namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions.Http.Caching;

    [Controller]
    [Authorize(Roles = RoleConsts.DevicesRole)]
    public class DeviceController : BaseMvcController
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly IUnitOfWork _uow;
        private readonly IJobControllerService _jobControllerService;
        private readonly IMemoryCacheHostedService _memCache;

        public DeviceController(
            ILogger<DeviceController> logger,
            IUnitOfWork uow,
            IJobControllerService jobControllerService,
            IMemoryCacheHostedService memCache)
        {
            _logger = logger;
            _uow = uow;
            _jobControllerService = jobControllerService;
            _memCache = memCache;
        }

        // GET: DeviceController
        public async Task<ActionResult> Index()
        {
            var devices = await _uow.Devices.FindAllAsync();
            var accountLevels = (await _uow.Accounts
                .FindAsync(account => devices.Any(device => device.AccountUsername == account.Username)))
                .ToDictionary(x => x.Username, y => y.Level);
            devices.ToList().ForEach(device =>
            {
                device.AccountLevel = device.AccountUsername != null
                    ? accountLevels[device.AccountUsername]
                    : (ushort)0;
            });
            var model = new ViewModelsModel<Device>
            {
                Items = devices.ToList(),
            };
            return View(model);
        }

        // GET: DeviceController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var device = await _uow.Devices.FindByIdAsync(id);
            if (device == null)
            {
                // Failed to retrieve device from database, does it exist?
                ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                CreateNotification(new NotificationViewModel
                {
                    Message = $"Device with id '{id}' does not exist.",
                    Icon = NotificationIcon.Error,
                });
                return View();
            }
            return View(device);
        }

        // GET: DeviceController/Create
        public async Task<ActionResult> Create()
        {
            var accountsInUse = (await _uow.Devices.FindAllAsync())
                .Select(device => device.AccountUsername)
                .ToList();
            var accounts = await _uow.Accounts.FindAsync(account => !accountsInUse.Contains(account.Username));
            var instances = await _uow.Instances.FindAllAsync();
            ViewBag.Instances = instances;
            ViewBag.Accounts = accounts;
            return View();
        }

        // POST: DeviceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Device deviceModel)//IFormCollection collection)
        {
            try
            {
                var uuid = deviceModel.Uuid;//Convert.ToString(collection["Uuid"]);
                var instanceName = deviceModel.InstanceName;//Convert.ToString(collection["InstanceName"]);
                var accountUsername = deviceModel.AccountUsername;//Convert.ToString(collection["AccountUsername"]);

                if (_uow.Devices.Any(device => device.Uuid == uuid))
                {
                    // Device exists already by name
                    ModelState.AddModelError("Device", $"Device with UUID '{uuid}' already exists.");
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Device with id '{uuid}' already exists.",
                        Icon = NotificationIcon.Error,
                    });

                    var accountsInUse = (await _uow.Devices.FindAllAsync())
                        .Select(device => device.AccountUsername)
                        .ToList();
                    var accounts = await _uow.Accounts.FindAsync(account => !accountsInUse.Contains(account.Username));
                    var instances = await _uow.Instances.FindAllAsync();
                    ViewBag.Instances = instances;
                    ViewBag.Accounts = accounts;
                    return View(deviceModel);
                }

                var device = new Device
                {
                    Uuid = uuid,
                    InstanceName = instanceName,
                    AccountUsername = accountUsername,
                };

                // Add device to database
                await _uow.Devices.AddAsync(device);
                await _uow.CommitAsync();

                _memCache.Set(device.Uuid, device);

                _jobControllerService.AddDevice(device);

                if (ModelState.IsValid)
                {
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Device '{device.Uuid}' has been created successfully!",
                        Icon = NotificationIcon.Success,
                    });
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Device", $"Unknown error occurred while creating new device.");
                CreateNotification(new NotificationViewModel
                {
                    Message = $"Unknown error occurred while creating device '{deviceModel.Uuid}'.",
                    Icon = NotificationIcon.Error,
                });

                var accountsInUse = (await _uow.Devices.FindAllAsync())
                    .Select(device => device.AccountUsername)
                    .ToList();
                var accounts = await _uow.Accounts.FindAsync(account => !accountsInUse.Contains(account.Username));
                var instances = await _uow.Instances.FindAllAsync();
                ViewBag.Instances = instances;
                ViewBag.Accounts = accounts;
                return View(deviceModel);
            }
        }

        // GET: DeviceController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var device = await _uow.Devices.FindByIdAsync(id);
            if (device == null)
            {
                // Failed to retrieve device from database, does it exist?
                ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                CreateNotification(new NotificationViewModel
                {
                    Message = $"Device with id '{id}' does not exist.",
                    Icon = NotificationIcon.Error,
                });
                return View();
            }

            var accountsInUse = (await _uow.Devices.FindAllAsync())
                .Select(device => device.AccountUsername)
                .ToList();
            // Filter accounts that are not used by devices unless this device we are editing
            var accounts = await _uow.Accounts.FindAsync(account => !accountsInUse.Contains(account.Username));
            var instances = await _uow.Instances.FindAllAsync();
            ViewBag.Instances = instances;
            ViewBag.Accounts = accounts;
            return View(device);
        }

        // POST: DeviceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var device = await _uow.Devices.FindByIdAsync(id);
                if (device == null)
                {
                    // Failed to retrieve device from database, does it exist?
                    ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Device with id '{id}' does not exist.",
                        Icon = NotificationIcon.Error,
                    });
                    return View();
                }

                var instanceName = Convert.ToString(collection["InstanceName"]);
                var accountUsername = Convert.ToString(collection["AccountUsername"]);

                // Check if device is not already assigned to instance before updating in database
                if (device.InstanceName != instanceName)
                {
                    device.InstanceName = instanceName;
                }

                // If the account assigned to the device changes, force device to logout/switch accounts
                if (device.AccountUsername != accountUsername)
                {
                    device.AccountUsername = accountUsername;
                    device.IsPendingAccountSwitch = true;
                }

                await _uow.Devices.UpdateAsync(device);
                await _uow.CommitAsync();

                _memCache.Set(device.Uuid, device);

                _jobControllerService.ReloadDevice(device, id);

                if (ModelState.IsValid)
                {
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Device '{device.Uuid}' has been updated successfully!",
                        Icon = NotificationIcon.Success,
                    });
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Device", $"Unknown error occurred while editing device '{id}'.");
                CreateNotification(new NotificationViewModel
                {
                    Message = $"Unknown error occurred while editing device '{id}'.",
                    Icon = NotificationIcon.Error,
                });
                return View();
            }
        }

        // GET: DeviceController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var device = await _uow.Devices.FindByIdAsync(id);
            if (device == null)
            {
                // Failed to retrieve device from database, does it exist?
                ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                CreateNotification(new NotificationViewModel
                {
                    Message = $"Device with id '{id}' does not exist.",
                    Icon = NotificationIcon.Error,
                });
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
                var device = await _uow.Devices.FindByIdAsync(id);
                if (device == null)
                {
                    // Failed to retrieve device from database, does it exist?
                    ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Device with id '{id}' does not exist.",
                        Icon = NotificationIcon.Error,
                    });
                    return View();
                }

                // Delete device from database
                await _uow.Devices.RemoveAsync(device);
                await _uow.CommitAsync();

                _memCache.Unset<string, Device>(id);

                _jobControllerService.RemoveDevice(id);

                if (ModelState.IsValid)
                {
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Device '{device.Uuid}' has been deleted successfully!",
                        Icon = NotificationIcon.Success,
                    });
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Device", $"Unknown error occurred while deleting device '{id}'.");
                CreateNotification(new NotificationViewModel
                {
                    Message = $"Unknown error occurred while deleting device '{id}'.",
                    Icon = NotificationIcon.Error,
                });
                return View();
            }
        }

        // GET: DeviceController/ForceAccountSwitch/5
        public async Task<ActionResult> ForceAccountSwitch(string id)
        {
            // Set Device.AccountUsername to null, GetJobTask will handle the
            // rest, no need for extra database column
            var device = await _uow.Devices.FindByIdAsync(id);
            if (device == null)
            {
                // Failed to retrieve device from database, does it exist?
                ModelState.AddModelError("Device", $"Device does not exist with id '{id}'.");
                CreateNotification(new NotificationViewModel
                {
                    Message = $"Device with id '{id}' does not exist.",
                    Icon = NotificationIcon.Error,
                });
                return View();
            }

            // Set assigned account for device to null so a new one is fetched upon next job request
            device.AccountUsername = null;
            device.IsPendingAccountSwitch = true;

            await _uow.Devices.UpdateAsync(device);
            await _uow.CommitAsync();

            CreateNotification(new NotificationViewModel
            {
                Message = $"Device with id '{id}' has been forced to logout of current account.",
                Icon = NotificationIcon.Success,
            });

            return RedirectToAction(nameof(Index));
        }
    }
}