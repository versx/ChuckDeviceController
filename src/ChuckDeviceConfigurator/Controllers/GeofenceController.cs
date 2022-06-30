namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Converters;
    using ChuckDeviceConfigurator.Models;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class GeofenceController : Controller
    {
        private readonly ILogger<GeofenceController> _logger;
        private readonly DeviceControllerContext _context;

        public GeofenceController(
            ILogger<GeofenceController> logger,
            DeviceControllerContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: GeofenceController
        public ActionResult Index()
        {
            var geofences = _context.Geofences.ToList();
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

            return View(geofence);
        }

        // GET: GeofenceController/Create
        public ActionResult Create()
        {
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
                var data = Convert.ToString(collection["Data"]);
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
            var geofence = await _context.Geofences.FindAsync(id);
            if (geofence == null)
            {
                // Failed to retrieve geofence from database, does it exist?
                ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
                return View();
            }

            var data = geofence.Data.Area;
            // Convert geofence area data to plain text to display
            dynamic area = geofence.Type == GeofenceType.Circle
                ? AreaConverters.CoordinatesToAreaString(data)
                : AreaConverters.MultiPolygonToAreaString(data);
            geofence.Data.Area = area;

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
                var data = Convert.ToString(collection["Data"]);
                var lines = data.Replace("<br>", "\r\n").Replace("\r\n", "\n");

                dynamic area = type == GeofenceType.Circle
                    ? AreaConverters.AreaStringToCoordinates(lines)
                    : AreaConverters.AreaStringToMultiPolygon(lines);

                /*
                if (!_context.Geofences.Any(fence => fence.Name == name))
                {
                    // Does not exist
                    return null;
                }
                */

                geofence.Name = name;
                geofence.Type = type;
                // TODO: Check if Data is null
                geofence.Data.Area = area;

                // Update geofence in database
                _context.Update(geofence);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Geofence", $"Unknown error occurred while editing geofence '{id}'.");
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