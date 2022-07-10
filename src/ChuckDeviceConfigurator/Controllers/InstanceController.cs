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
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Name"]);
                var type = (InstanceType)Convert.ToUInt16(collection["Type"]);
                var minLevel = Convert.ToUInt16(collection["MinimumLevel"]);
                var maxLevel = Convert.ToUInt16(collection["MaximumLevel"]);
                var geofences = Convert.ToString(collection["Geofences"]).Split(',').ToList();
                var accountGroup = Convert.ToString(collection["Data.AccountGroup"]);
                var isEvent = collection["Data.IsEvent"].Contains("true");

                var circleRouteType = (Convert.ToString(collection["Data.CircleRouteType"]) ?? "Default").StringToObject<CircleInstanceRouteType>();

                var questMode = (Convert.ToString(collection["Data.QuestMode"]) ?? "Normal").StringToObject<QuestMode>();
                var timeZone = Convert.ToString(collection["Data.TimeZone"]);
                var enableDst = collection["Data.EnableDst"].Contains("true");
                var spinLimit = Convert.ToUInt16(collection["Data.SpinLimit"]);
                var useWarningAccounts = collection["Data.UseWarningAccounts"].Contains("true");
                var ignoreS2CellBootstrap = collection["Data.IgnoreS2CellBootstrap"].Contains("true");

                var circleSize = Convert.ToString(collection["Data.CircleSize"]);
                var fastBootstrapMode = collection["Data.FastBootstrap"].Contains("true");

                var ivList = Convert.ToString(collection["Data.IvList"]);
                var ivQueueLimit = Convert.ToUInt16(Convert.ToString(collection["Data.IvQueueLimit"]) ?? "100");
                var enableLureEncounters = collection["Data.EnableLureEncounters"].Contains("true");

                if (_context.Instances.Any(inst => inst.Name == name))
                {
                    // Instance exists already by name
                    ModelState.AddModelError("Instance", $"Instance with name '{name}' already exists.");
                    return View();
                }

                var instance = new Instance
                {
                    Name = name,
                    Type = type,
                    MinimumLevel = minLevel,
                    MaximumLevel = maxLevel,
                    Geofences = geofences,
                    Data = new InstanceData
                    {
                        QuestMode = questMode,
                        TimeZone = timeZone,
                        EnableDst = enableDst,
                        SpinLimit = spinLimit,
                        UseWarningAccounts = useWarningAccounts,
                        IgnoreS2CellBootstrap = ignoreS2CellBootstrap,
                        
                        FastBootstrapMode = fastBootstrapMode,
                        
                        CircleRouteType = circleRouteType,
                        CircleSize = Convert.ToUInt16(circleSize == "" ? "70" : circleSize),

                        IvList = ivList,
                        IvQueueLimit = ivQueueLimit,
                        EnableLureEncounters = enableLureEncounters,

                        AccountGroup = accountGroup,
                        IsEvent = false,
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

            var model = new EditInstanceViewModel
            {
                Name = instance.Name,
                Type = instance.Type,
                MinimumLevel = instance.MinimumLevel,
                MaximumLevel = instance.MaximumLevel,
                Geofences = instance.Geofences,
                Data = new EditInstanceDataViewModel
                {
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
        public async Task<ActionResult> Edit(string id, EditInstanceViewModel model) //IFormCollection collection)
        {
            try
            {
                var instance = await _context.Instances.FindAsync(id);
                if (instance == null)
                {
                    // Failed to retrieve instance from database, does it exist?
                    ModelState.AddModelError("Instance", $"Instance does not exist with id '{id}'.");
                    return View();
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
                instance.Data.QuestMode = model.Data.QuestMode;
                instance.Data.TimeZone = model.Data.TimeZone;
                instance.Data.EnableDst = model.Data.EnableDst;
                instance.Data.SpinLimit = model.Data.SpinLimit;
                instance.Data.UseWarningAccounts = model.Data.UseWarningAccounts;
                instance.Data.IgnoreS2CellBootstrap = model.Data.IgnoreS2CellBootstrap;

                instance.Data.FastBootstrapMode = model.Data.FastBootstrapMode;
                instance.Data.CircleSize = model.Data.CircleSize;

                instance.Data.CircleRouteType = model.Data.CircleRouteType;

                instance.Data.IvList = model.Data.IvList;
                instance.Data.IvQueueLimit = model.Data.IvQueueLimit;
                instance.Data.EnableLureEncounters = model.Data.EnableLureEncounters;

                instance.Data.AccountGroup = model.Data.AccountGroup;
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
                return View();
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