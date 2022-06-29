namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Models;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class InstanceController : Controller
    {
        private readonly DeviceControllerContext _context;

        public InstanceController(DeviceControllerContext context)
        {
            _context = context;
        }

        // GET: InstanceController
        public ActionResult Index()
        {
            var instances = _context.Instances.ToList();
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
            return View(instance);
        }

        // GET: InstanceController/Create
        public ActionResult Create()
        {
            var model = new ViewInstanceModel
            {
                InstanceTypes = Enum.GetNames(typeof(InstanceType)).ToList(),
                Geofences = new List<string>
                {
                    "Test",
                    "Test2",
                },
            };
            return View(model);
        }

        // POST: InstanceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Instance.Name"]);
                var type = (InstanceType)Convert.ToUInt16(collection["Instance.Type"]);
                var minLevel = Convert.ToUInt16(collection["Instance.MinimumLevel"]);
                var maxLevel = Convert.ToUInt16(collection["Instance.MaximumLevel"]);
                var geofences = Convert.ToString(collection["Instance.Geofences"]).Split(',').ToList();
                var accountGroup = Convert.ToString(collection["Instance.Data.AccountGroup"]);
                var isEvent = collection["Instance.Data.IsEvent"].Contains("true");
                if (_context.Instances.Any(inst => inst.Name == name))
                {
                    // TODO: Exists already
                    return null;
                }
                else
                {
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
                    await _context.Instances.AddAsync(instance);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: InstanceController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var instance = await _context.Instances.FindAsync(id);
            var model = new ViewInstanceModel
            {
                InstanceTypes = Enum.GetNames(typeof(InstanceType)).ToList(),
                Geofences = new List<string>
                {
                    "Test",
                    "Test2",
                },
                Instance = instance,
            };
            return View(model);
        }

        // POST: InstanceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Instance.Name"]);
                var type = (InstanceType)Convert.ToUInt16(collection["Instance.Type"]);
                var minLevel = Convert.ToUInt16(collection["Instance.MinimumLevel"]);
                var maxLevel = Convert.ToUInt16(collection["Instance.MaximumLevel"]);
                var geofences = Convert.ToString(collection["Instance.Geofences"]).Split(',').ToList();
                var accountGroup = Convert.ToString(collection["Instance.Data.AccountGroup"]);
                var isEvent = collection["Instance.Data.IsEvent"].Contains("true");
                if (_context.Instances.Any(inst => inst.Name == id))
                {
                    // TODO: Exists already with new name
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
                    _context.Instances.Update(instance);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // TODO: Instance does not exist
                    return null;
                }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: InstanceController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var instance = await _context.Instances.FindAsync(id);
            return View(instance);
        }

        // POST: InstanceController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                if (!_context.Instances.Any(inst => inst.Name == id))
                {
                    // Not found
                    return null;
                }

                var instance = await _context.Instances.FindAsync(id);
                _context.Instances.Remove(instance);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}