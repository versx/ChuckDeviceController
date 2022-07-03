namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;

    using ChuckDeviceConfigurator.Services.Jobs;
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

        public InstanceController(
            ILogger<InstanceController> logger,
            DeviceControllerContext context,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _context = context;
            _jobControllerService = jobControllerService;
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
            var selectedGeofences = geofences.Select(g => new SelectListItem("Name", g.Name, instance.Geofences.Contains(g.Name)))
                                             .ToList();
            ViewBag.Geofences = new MultiSelectList(geofences, "Name", "Name", selectedGeofences);
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
                var isEvent = collection["Data.IsEvent"].Contains("true");

                instance.Name = name;
                instance.Type = type;
                instance.MinimumLevel = minLevel;
                instance.MaximumLevel = maxLevel;
                instance.Geofences = geofences;
                // TODO: Check if Data is null
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