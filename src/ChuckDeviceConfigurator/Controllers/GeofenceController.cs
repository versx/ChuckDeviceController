namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    using ChuckDeviceConfigurator.Services.Geofences;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Geometry.Converters;
    using ChuckDeviceController.Geometry.Models;

    [Authorize(Roles = RoleConsts.GeofencesRole)]
    public class GeofenceController : Controller
    {
        private readonly ILogger<GeofenceController> _logger;
        private readonly ControllerDbContext _context;
        private readonly IGeofenceControllerService _geofenceService;
        private readonly IJobControllerService _jobControllerService;
        private readonly LeafletMapConfig _mapConfig;

        public GeofenceController(
            ILogger<GeofenceController> logger,
            ControllerDbContext context,
            IGeofenceControllerService geofenceService,
            IJobControllerService jobControllerService,
            IOptions<LeafletMapConfig> mapConfig)
        {
            _logger = logger;
            _context = context;
            _geofenceService = geofenceService;
            _jobControllerService = jobControllerService;
            _mapConfig = mapConfig.Value;
        }

        // GET: GeofenceController
        public ActionResult Index()
        {
            var geofences = _context.Geofences.ToList();
            foreach (var geofence in geofences)
            {
                string area = Convert.ToString(geofence.Data.Area);
                var areasCount = geofence.Type == GeofenceType.Circle
                    ? area?.FromJson<List<Coordinate>>()?.Count ?? 0
                    : area?.FromJson<List<List<Coordinate>>>()?.Count ?? 0;
                geofence.AreasCount = (uint)areasCount;
            }
            return View(new ViewModelsModel<Geofence>
            {
                Items = geofences,
            });
        }

        // GET: GeofenceController/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var geofence = await _context.Geofences.FindAsync(id);
            if (geofence == null)
            {
                // Failed to retrieve geofence from database, does it exist?
                ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
                return View();
            }

            ViewData["GeofenceData"] = geofence.ConvertToIni();
            ViewData["MapConfig"] = _mapConfig.ToJson();
            return View(geofence);
        }

        // GET: GeofenceController/Create
        public ActionResult Create()
        {
            var geofenceNames = _context.Geofences.ToList().Select(x => x.Name);
            ViewData["GeofenceNames"] = geofenceNames;
            ViewData["MapConfig"] = _mapConfig.ToJson();
            return View();
        }

        // POST: GeofenceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var name = Convert.ToString(collection["Name"]);
                var type = (GeofenceType)Convert.ToUInt16(collection["Type"]);
                var data = Convert.ToString(collection["Data.Area"]);
                var lines = data.Replace("<br>", "\r\n").Replace("\r\n", "\n");

                dynamic area = type == GeofenceType.Circle
                    ? AreaConverters.AreaStringToCoordinates(lines)
                    : AreaConverters.AreaStringToMultiPolygon(lines);

                if (_context.Geofences.Any(fence => fence.Name == name))
                {
                    // Geofence already exists by name
                    ModelState.AddModelError("Geofence", $"Geofence with name '{name}' already exists.");
                    return View();
                }
                var geofence = new Geofence
                {
                    Name = name,
                    Type = type,
                    Data = new GeofenceData
                    {
                        Area = area,
                    },
                };

                // Add geofence to database
                await _context.AddAsync(geofence);
                await _context.SaveChangesAsync();

                _geofenceService.Add(geofence);

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Geofence", $"Unknown error occurred while creating new geofence.");
                return View();
            }
        }

        // GET: GeofenceController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var geofenceNames = _context.Geofences.ToList().Select(x => x.Name);
            var geofence = await _context.Geofences.FindAsync(id);
            if (geofence == null)
            {
                // Failed to retrieve geofence from database, does it exist?
                ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
                return View();
            }

            geofence.Data ??= new();
            var data = geofence.Data.Area;
            // Convert geofence area data to plain text to display
            dynamic area = geofence.Type == GeofenceType.Circle
                ? AreaConverters.CoordinatesToAreaString(data)
                : AreaConverters.MultiPolygonToAreaString(data);
            geofence.Data.Area = area;
            ViewData["GeofenceNames"] = geofenceNames;
            ViewData["MapConfig"] = _mapConfig.ToJson();
            return View(geofence);
        }

        // POST: GeofenceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
        {
            try
            {
                var geofence = await _context.Geofences.FindAsync(id);
                if (geofence == null)
                {
                    // Failed to retrieve geofence from database, does it exist?
                    ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
                    return View();
                }

                var name = Convert.ToString(collection["Name"]);
                var type = (GeofenceType)Convert.ToUInt16(collection["Type"]);
                var data = Convert.ToString(collection["Data.Area"]);
                var lines = data.Replace("<br>", "\r\n").Replace("\r\n", "\n");

                dynamic area = type == GeofenceType.Circle
                    ? AreaConverters.AreaStringToCoordinates(lines)
                    : AreaConverters.AreaStringToMultiPolygon(lines);

                geofence.Name = name;
                geofence.Type = type;
                geofence.Data ??= new();
                geofence.Data.Area = area;

                // Update geofence in database
                _context.Update(geofence);
                await _context.SaveChangesAsync();

                _geofenceService.Edit(geofence, id);

                // Get list of instances that have geofence
                var instancesWithGeofence = _context.Instances
                    .AsEnumerable()
                    .Where(instance => instance.Geofences.Contains(geofence.Name))
                    .ToList();
                foreach (var instance in instancesWithGeofence)
                {
                    // Reload instance so updated geofence is applied
                    await _jobControllerService.ReloadInstanceAsync(instance, instance.Name);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Geofence", $"Unknown error occurred while editing geofence '{id}': {ex.Message}");
                return View();
            }
        }

        // GET: GeofenceController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var geofence = await _context.Geofences.FindAsync(id);
            if (geofence == null)
            {
                // Failed to retrieve geofence from database, does it exist?
                ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
                return View();
            }

            return View(geofence);
        }

        // POST: GeofenceController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                var geofence = await _context.Geofences.FindAsync(id);
                if (geofence == null)
                {
                    // Failed to retrieve geofence from database, does it exist?
                    ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
                    return View(geofence);
                }

                // Delete geofence from database
                _context.Geofences.Remove(geofence);
                await _context.SaveChangesAsync();

                _geofenceService.Delete(id);

                // Get list of instances that have geofence
                var instancesWithGeofence = _context.Instances.Where(instance => instance.Geofences.Contains(geofence.Name))
                                                              .ToList();
                foreach (var instance in instancesWithGeofence)
                {
                    instance.Geofences.Remove(geofence.Name);
                    await _context.SaveChangesAsync();

                    // Reload instance so removed geofence is not ignored
                    await _jobControllerService.ReloadInstanceAsync(instance, instance.Name);
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Geofence", $"Unknown error occurred while deleting geofence '{id}'.");
                return View();
            }
        }
    }
}