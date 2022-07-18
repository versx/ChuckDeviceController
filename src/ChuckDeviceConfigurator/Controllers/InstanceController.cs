namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.TimeZone;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    [Authorize(Roles = RoleConsts.InstancesRole)]
    public class InstanceController : Controller
    {
        private readonly ILogger<InstanceController> _logger;
        private readonly DeviceControllerContext _context;
        private readonly IJobControllerService _jobControllerService;
        private readonly ITimeZoneService _timeZoneService;

        public InstanceController(
            ILogger<InstanceController> logger,
            DeviceControllerContext context,
            IJobControllerService jobControllerService,
            ITimeZoneService timeZoneService)
        {
            _logger = logger;
            _context = context;
            _jobControllerService = jobControllerService;
            _timeZoneService = timeZoneService;
        }

        // GET: InstanceController
        public async Task<ActionResult> Index()
        {
            var instances = _context.Instances.ToList();
            var devices = _context.Devices.ToList();
            foreach (var instance in instances)
            {
                var deviceCount = devices.Count(device => device.InstanceName == instance.Name);
                instance.DeviceCount += deviceCount;
                var status = await _jobControllerService.GetStatusAsync(instance);
                instance.Status = status;
            }
            var model = new ViewModelsModel<Instance>
            {
                Items = instances,
            };
            Response.Headers["Refresh"] = "5"; // TODO: Make table refresh configurable (implement a better way actually)
            return View(model);
        }

        // GET: InstanceController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance == null)
            {
                // Failed to retrieve instance from database, does it exist?
                ModelState.AddModelError("Instance", $"Instance does not exist with id '{id}'.");
                return View();
            }

            var status = await _jobControllerService.GetStatusAsync(instance);
            instance.Status = status;

            return View(instance);
        }

        // GET: InstanceController/Create
        public ActionResult Create()
        {
            ViewBag.Geofences = _context.Geofences.ToList();
            ViewBag.Instances = _context.Instances.ToList();
            ViewBag.IvLists = _context.IvLists.ToList();
            ViewBag.TimeZones = _timeZoneService.TimeZones.Select(pair => new { Name = pair.Key }).ToList();
            return View();
        }

        // POST: InstanceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ManageInstanceViewModel model)
        {
            try
            {
                if (_context.Instances.Any(inst => inst.Name == model.Name))
                {
                    // Instance exists already by name
                    ModelState.AddModelError("Instance", $"Instance with name '{model.Name}' already exists.");
                    return View();
                }

                var instance = new Instance
                {
                    Name = model.Name,
                    Type = model.Type,
                    MinimumLevel = model.MinimumLevel,
                    MaximumLevel = model.MaximumLevel,
                    Geofences = model.Geofences,
                    Data = PopulateInstanceDataFromModel(model.Data),
                };

                // Add instance to database
                await _context.Instances.AddAsync(instance);
                await _context.SaveChangesAsync();

                await _jobControllerService.AddInstanceAsync(instance);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Instance", $"Unknown error occurred while creating new instance.");
                return View();
            }
        }

        // GET: InstanceController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance == null)
            {
                // Failed to retrieve instance from database, does it exist?
                ModelState.AddModelError("Instance", $"Instance does not exist with id '{id}'.");
                return View();
            }

            var model = new ManageInstanceViewModel
            {
                Name = instance.Name,
                Type = instance.Type,
                MinimumLevel = instance.MinimumLevel,
                MaximumLevel = instance.MaximumLevel,
                Geofences = instance.Geofences,
                Data = new ManageInstanceDataViewModel
                {
                    AccountGroup = instance.Data?.AccountGroup ?? Strings.DefaultAccountGroup,
                    IsEvent = instance.Data?.IsEvent ?? Strings.DefaultIsEvent,
                    UseWarningAccounts = instance.Data?.UseWarningAccounts ?? Strings.DefaultUseWarningAccounts,
                    CircleRouteType = instance.Data?.CircleRouteType ?? Strings.DefaultCircleRouteType,
                    OptimizeDynamicRoute = instance.Data?.OptimizeDynamicRoute ?? Strings.DefaultOptimizeDynamicRoute,
                    CircleSize = instance.Data?.CircleSize ?? Strings.DefaultCircleSize,
                    EnableDst = instance.Data?.EnableDst ?? Strings.DefaultEnableDst,
                    EnableLureEncounters = instance.Data?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters,
                    FastBootstrapMode = instance.Data?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode,
                    IgnoreS2CellBootstrap = instance.Data?.IgnoreS2CellBootstrap ?? Strings.DefaultIgnoreS2CellBootstrap,
                    IvList = instance.Data?.IvList ?? Strings.DefaultIvList,
                    IvQueueLimit = instance.Data?.IvQueueLimit ?? Strings.DefaultIvQueueLimit,
                    QuestMode = instance.Data?.QuestMode ?? Strings.DefaultQuestMode,
                    SpinLimit = instance.Data?.SpinLimit ?? Strings.DefaultSpinLimit,
                    TimeZone = instance.Data?.TimeZone ?? Strings.DefaultTimeZone,
                    LogoutDelay = instance.Data?.LogoutDelay ?? Strings.DefaultLogoutDelay,
                    MaximumSpinAttempts = instance.Data?.MaximumSpinAttempts ?? Strings.DefaultMaximumSpinAttempts,
                    BootstrapCompleteInstanceName = instance.Data?.BootstrapCompleteInstanceName,
                },
            };

            ViewBag.Geofences = _context.Geofences.ToList();// new MultiSelectList(geofences, "Name", "Name", selectedGeofences);
            ViewBag.Instances = _context.Instances.ToList();
            ViewBag.IvLists = _context.IvLists.ToList();
            ViewBag.TimeZones = _timeZoneService.TimeZones.Select(pair => new { Name = pair.Key });
            return View(model);
        }

        // POST: InstanceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, ManageInstanceViewModel model)
        {
            try
            {
                var instance = await _context.Instances.FindAsync(id);
                if (instance == null)
                {
                    // Failed to retrieve instance from database, does it exist?
                    ModelState.AddModelError("Instance", $"Instance does not exist with id '{id}'.");
                    return View(model);
                }

                // Check if new name already exists and not current instance
                if (_context.Instances.Any(inst => inst.Name == model.Name && inst.Name != id))
                {
                    ModelState.AddModelError("Instance", $"Instance with name '{model.Name}' already exists, please choose a different name.");
                    return View(model);
                }

                instance.Name = model.Name;
                instance.Type = model.Type;
                instance.MinimumLevel = model.MinimumLevel;
                instance.MaximumLevel = model.MaximumLevel;
                instance.Geofences = model.Geofences;
                instance.Data = PopulateInstanceDataFromModel(model.Data);

                // Update instance in database
                _context.Instances.Update(instance);
                await _context.SaveChangesAsync();

                await _jobControllerService.ReloadInstanceAsync(instance, id);
                
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Instance", $"Unknown error occurred while editing instance '{id}'.");
                return View(model);
            }
        }

        // GET: InstanceController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance == null)
            {
                // Failed to retrieve instance from database, does it exist?
                ModelState.AddModelError("Instance", $"Instance does not exist with id '{id}'.");
                return View();
            }
            return View(instance);
        }

        // POST: InstanceController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var instance = await _context.Instances.FindAsync(id);
                if (instance == null)
                {
                    // Failed to retrieve instance from database, does it exist?
                    return null;
                }

                // Delete instance from database
                _context.Instances.Remove(instance);
                await _context.SaveChangesAsync();

                await _jobControllerService.RemoveInstanceAsync(id);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Instance", $"Unknown error occurred while deleting instance '{id}'.");
                return View();
            }
        }

        // GET: InstanceController/IVQueue/test
        [Route("/Instance/IvQueue/{name}")]
        public ActionResult IvQueue(string name)
        {
            var ivQueue = _jobControllerService.GetIvQueue(name);
            var model = new IvQueueViewModel
            {
                Name = name,
                Queue = ivQueue.ToList(),
            };
            return View(model);
        }

        private static InstanceData PopulateInstanceDataFromModel(ManageInstanceDataViewModel model)
        {
            var instanceData = new InstanceData
            {
                // Quests
                QuestMode = model?.QuestMode ?? Strings.DefaultQuestMode,
                TimeZone = model?.TimeZone ?? Strings.DefaultTimeZone,
                EnableDst = model?.EnableDst ?? Strings.DefaultEnableDst,
                SpinLimit = model?.SpinLimit ?? Strings.DefaultSpinLimit,
                UseWarningAccounts = model?.UseWarningAccounts ?? Strings.DefaultUseWarningAccounts,
                IgnoreS2CellBootstrap = model?.IgnoreS2CellBootstrap ?? Strings.DefaultIgnoreS2CellBootstrap,
                LogoutDelay = model?.LogoutDelay ?? Strings.DefaultLogoutDelay,
                MaximumSpinAttempts = model?.MaximumSpinAttempts ?? Strings.DefaultMaximumSpinAttempts,

                // Bootstrap
                FastBootstrapMode = model?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode,
                CircleSize = model?.CircleSize ?? Strings.DefaultCircleSize,
                OptimizeBootstrapRoute = model?.OptimizeBootstrapRoute ?? Strings.DefaultOptimizeBootstrapRoute,
                BootstrapCompleteInstanceName = model?.BootstrapCompleteInstanceName ?? Strings.DefaultBootstrapCompleteInstanceName,

                // Circle
                CircleRouteType = model?.CircleRouteType ?? Strings.DefaultCircleRouteType,
                OptimizeDynamicRoute = model?.OptimizeDynamicRoute ?? Strings.DefaultOptimizeDynamicRoute,

                // IV
                IvList = model?.IvList ?? Strings.DefaultIvList,
                IvQueueLimit = model?.IvQueueLimit ?? Strings.DefaultIvQueueLimit,
                EnableLureEncounters = model?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters,

                // Spawnpoint
                OnlyUnknownSpawnpoints = model?.OnlyUnknownSpawnpoints ?? Strings.DefaultOnlyUnknownSpawnpoints,
                OptimizeSpawnpointsRoute = model?.OptimizeSpawnpointsRoute ?? Strings.DefaultOptimizeSpawnpointRoute,

                // All
                AccountGroup = model?.AccountGroup ?? Strings.DefaultAccountGroup,
                IsEvent = model?.IsEvent ?? Strings.DefaultIsEvent,
            };
            return instanceData;
        }
    }
}