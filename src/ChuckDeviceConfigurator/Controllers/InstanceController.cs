namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.TimeZone;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

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
            ViewBag.IvLists = _context.IvLists.ToList();
            ViewBag.TimeZones = _timeZoneService.TimeZones.Select(pair => new { Name = pair.Key }).ToList();
            return View();
        }

        // POST: InstanceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ManageInstanceViewModel model) //IFormCollection collection)
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
                    Data = new InstanceData
                    {
                        QuestMode = model.Data?.QuestMode ?? Strings.DefaultQuestMode,
                        TimeZone = model.Data?.TimeZone ?? Strings.DefaultTimeZone,
                        EnableDst = model.Data?.EnableDst ?? Strings.DefaultEnableDst,
                        SpinLimit = model.Data?.SpinLimit ?? Strings.DefaultSpinLimit,
                        UseWarningAccounts = model.Data?.UseWarningAccounts ?? Strings.DefaultUseWarningAccounts,
                        IgnoreS2CellBootstrap = model.Data?.IgnoreS2CellBootstrap ?? Strings.DefaultIgnoreS2CellBootstrap,
                        
                        FastBootstrapMode = model.Data?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode,
                        
                        CircleRouteType = model.Data?.CircleRouteType ?? Strings.DefaultCircleRouteType,
                        CircleSize = model.Data?.CircleSize ?? Strings.DefaultCircleSize,

                        IvList = model.Data?.IvList ?? Strings.DefaultIvList,
                        IvQueueLimit = model.Data?.IvQueueLimit ?? Strings.DefaultIvQueueLimit,
                        EnableLureEncounters = model.Data?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters,

                        AccountGroup = model.Data?.AccountGroup ?? Strings.DefaultAccountGroup,
                        IsEvent = model.Data?.IsEvent ?? Strings.DefaultIsEvent,
                    },
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
                    // TODO: Group default instance property values somewhere
                    AccountGroup = instance.Data?.AccountGroup ?? null,
                    IsEvent = instance.Data?.IsEvent ?? false,
                    UseWarningAccounts = instance.Data?.UseWarningAccounts ?? false,
                    CircleRouteType = instance.Data?.CircleRouteType ?? CircleInstanceRouteType.Default,
                    CircleSize = instance.Data?.CircleSize ?? 70,
                    EnableDst = instance.Data?.EnableDst ?? false,
                    EnableLureEncounters = instance.Data?.EnableLureEncounters ?? false,
                    FastBootstrapMode = instance.Data?.FastBootstrapMode ?? false,
                    IgnoreS2CellBootstrap = instance.Data?.IgnoreS2CellBootstrap ?? false,
                    IvList = instance.Data?.IvList ?? null,
                    IvQueueLimit = instance.Data?.IvQueueLimit ?? 100,
                    QuestMode = instance.Data?.QuestMode ?? QuestMode.Normal,
                    SpinLimit = instance.Data?.SpinLimit ?? 3500,
                    TimeZone = instance.Data?.TimeZone ?? null,
                },
            };

            ViewBag.Geofences = _context.Geofences.ToList();// new MultiSelectList(geofences, "Name", "Name", selectedGeofences);
            ViewBag.IvLists = _context.IvLists.ToList();
            ViewBag.TimeZones = _timeZoneService.TimeZones.Select(pair => new { Name = pair.Key });
            return View(model);
        }

        // POST: InstanceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, ManageInstanceViewModel model) //IFormCollection collection)
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
                if (instance.Data == null)
                {
                    instance.Data = new InstanceData();
                }
                instance.Data.QuestMode = model.Data.QuestMode ?? Strings.DefaultQuestMode;
                instance.Data.TimeZone = model.Data.TimeZone ?? Strings.DefaultTimeZone;
                instance.Data.EnableDst = model.Data.EnableDst;
                instance.Data.SpinLimit = model.Data.SpinLimit ?? Strings.DefaultSpinLimit;
                instance.Data.UseWarningAccounts = model.Data.UseWarningAccounts;
                instance.Data.IgnoreS2CellBootstrap = model.Data.IgnoreS2CellBootstrap;

                instance.Data.FastBootstrapMode = model.Data.FastBootstrapMode;
                instance.Data.CircleSize = model.Data.CircleSize ?? Strings.DefaultCircleSize;

                instance.Data.CircleRouteType = model.Data.CircleRouteType ?? Strings.DefaultCircleRouteType;

                instance.Data.IvList = model.Data.IvList ?? Strings.DefaultIvList;
                instance.Data.IvQueueLimit = model.Data.IvQueueLimit ?? Strings.DefaultIvQueueLimit;
                instance.Data.EnableLureEncounters = model.Data.EnableLureEncounters;

                instance.Data.AccountGroup = model.Data.AccountGroup ?? Strings.DefaultAccountGroup;
                instance.Data.IsEvent = model.Data.IsEvent;

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
    }
}