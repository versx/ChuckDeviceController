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
        private readonly DeviceControllerContext _context;

        public GeofenceController(DeviceControllerContext context)
        {
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
                    // Already exists
                    return null;
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
                await _context.AddAsync(geofence);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: GeofenceController/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var geofence = await _context.Geofences.FindAsync(id);
            return View(geofence);
        }

        // POST: GeofenceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, IFormCollection collection)
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

                if (!_context.Geofences.Any(fence => fence.Name == name))
                {
                    // Does not exist
                    return null;
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
                _context.Update(geofence);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: GeofenceController/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var geofence = await _context.Geofences.FindAsync(id);
            return View(geofence);
        }

        // POST: GeofenceController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                if (!_context.Geofences.Any(fence => fence.Name == id))
                {
                    // Does not exist
                    return null;
                }

                var geofence = await _context.Geofences.FindAsync(id);
                _context.Geofences.Remove(geofence);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
