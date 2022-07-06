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
        public ActionResult Index()
        {
            var instances = _context.Instances.ToList();
            var devices = _context.Devices.ToList();
            instances.ForEach(async instance =>
            {
                devices.ForEach(device =>
                {
                    if (device.InstanceName == instance.Name)
                    {
                        instance.DeviceCount++;
                    }
                });
                var status = await _jobControllerService.GetStatusAsync(instance);
                instance.Status = status;
            });
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

                var circleRouteType = (CircleInstanceRouteType)Convert.ToUInt16(collection["Data.CircleRouteType"]);

                var questMode = Convert.ToString(collection["Data.QuestMode"]);
                var timeZone = Convert.ToString(collection["Data.TimeZone"]);
                var enableDst = collection["Data.EnableDst"].Contains("true");
                var spinLimit = Convert.ToUInt16(collection["Data.SpinLimit"]);
                var useWarningAccounts = collection["Data.UseWarningAccounts"].Contains("true");
                var ignoreS2CellBootstrap = collection["Data.IgnoreS2CellBootstrap"].Contains("true");

                var circleSize = Convert.ToUInt16(Convert.ToString(collection["Data.CircleSize"]) ?? "70");
                var fastBootstrapMode = collection["Data.FastBootstrap"].Contains("true");

                var ivQueueLimit = Convert.ToUInt16(Convert.ToString(collection["Data.IvQueueLimit"]) ?? "100");

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
                        CircleSize = circleSize,

                        //IvList = null,
                        IvQueueLimit = ivQueueLimit,

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

            var geofences = _context.Geofences.ToList();
            /*
            var selectedGeofences = geofences.Select(g => new SelectListItem("Name", g.Name, instance.Geofences.Contains(g.Name)))
                                             .ToList();
            */
            ViewBag.Geofences = geofences;// new MultiSelectList(geofences, "Name", "Name", selectedGeofences);
            ViewBag.TimeZones = _timeZoneService.TimeZones.Select(pair => new { Name = pair.Key });
            return View(instance);
        }

        // POST: InstanceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
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

                var name = Convert.ToString(collection["Name"]);
                var type = (InstanceType)Convert.ToUInt16(collection["Type"]);
                var minLevel = Convert.ToUInt16(collection["MinimumLevel"]);
                var maxLevel = Convert.ToUInt16(collection["MaximumLevel"]);
                var geofences = Convert.ToString(collection["Geofences"]).Split(',').ToList();
                var accountGroup = Convert.ToString(collection["Data.AccountGroup"]);
                var isEvent = collection["Data.IsEvent"].Contains("on");

                var circleRouteType = (CircleInstanceRouteType)Convert.ToUInt16(Convert.ToString(collection["Data.CircleRouteType"]) ?? "0");

                var questMode = (QuestMode)Convert.ToUInt16(Convert.ToString(collection["Daa.QuestMode"]) ?? "0");
                var timeZone = Convert.ToString(collection["Data.TimeZone"]);
                var enableDst = collection["Data.EnableDst"].Contains("on");
                var spinLimit = Convert.ToUInt16(collection["Data.SpinLimit"]);
                var useWarningAccounts = collection["Data.UseWarningAccounts"].Contains("on");
                var ignoreS2CellBootstrap = collection["Data.IgnoreS2CellBootstrap"].Contains("on");

                var circleSize = Convert.ToUInt16(Convert.ToString(collection["Data.CircleSize"]) ?? "70");
                var fastBootstrapMode = collection["Data.FastBootstrap"].Contains("on");

                var ivQueueLimit = Convert.ToUInt16(Convert.ToString(collection["Data.IvQueueLimit"]) ?? "100");

                instance.Name = name;
                instance.Type = type;
                instance.MinimumLevel = minLevel;
                instance.MaximumLevel = maxLevel;
                instance.Geofences = geofences;
                if (instance.Data == null)
                {
                    instance.Data = new InstanceData();
                }
                instance.Data.QuestMode = Convert.ToString(questMode);
                instance.Data.TimeZone = timeZone;
                instance.Data.EnableDst = enableDst;
                instance.Data.SpinLimit = spinLimit;
                instance.Data.UseWarningAccounts = useWarningAccounts;
                instance.Data.IgnoreS2CellBootstrap = ignoreS2CellBootstrap;

                instance.Data.FastBootstrapMode = fastBootstrapMode;
                instance.Data.CircleSize = circleSize;

                instance.Data.CircleRouteType = circleRouteType;

                //IvList = null;
                instance.Data.IvQueueLimit = ivQueueLimit;

                instance.Data.AccountGroup = accountGroup;
                instance.Data.IsEvent = false;

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