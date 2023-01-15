namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using ChuckDeviceConfigurator.Services.Geofences;
using ChuckDeviceConfigurator.Services.Jobs;
using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Geometry.Converters;

[Authorize(Roles = RoleConsts.GeofencesRole)]
public class GeofenceController : Controller
{
    private readonly ILogger<GeofenceController> _logger;
    private readonly IUnitOfWork _uow;
    private readonly IGeofenceControllerService _geofenceService;
    private readonly IJobControllerService _jobControllerService;
    private readonly LeafletMapConfig _mapConfig;

    public GeofenceController(
        ILogger<GeofenceController> logger,
        IUnitOfWork uow,
        IGeofenceControllerService geofenceService,
        IJobControllerService jobControllerService,
        IOptions<LeafletMapConfig> mapConfig)
    {
        _logger = logger;
        _uow = uow;
        _geofenceService = geofenceService;
        _jobControllerService = jobControllerService;
        _mapConfig = mapConfig.Value;
    }

    // GET: GeofenceController
    public async Task<ActionResult> Index()
    {
        var geofences = await _uow.Geofences.FindAllAsync();
        return View(new ViewModelsModel<Geofence>
        {
            Items = geofences.ToList(),
        });
    }

    // GET: GeofenceController/Details/5
    public async Task<ActionResult> Details(string id)
    {
        var geofence = await _uow.Geofences.FindByIdAsync(id);
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
    public async Task<ActionResult> Create()
    {
        var geofenceNames = (await _uow.Geofences.FindAllAsync()).Select(x => x.Name);
        ViewData["GeofenceNames"] = geofenceNames;
        ViewData["MapConfig"] = _mapConfig.ToJson();
        return View();
    }

    // POST: GeofenceController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(IFormCollection collection)
    {
        Geofence? geofence = null;
        try
        {
            var name = Convert.ToString(collection["Name"]);
            //var type = (GeofenceType)Convert.ToUInt16(collection["Type"]);
            var type = GeofenceType.StringToGeofenceType(collection["Type"].ToString());
            var data = Convert.ToString(collection["Data.Area"]);
            var lines = data.Replace("<br>", "\r\n").Replace("\r\n", "\n");

            object area = type == GeofenceType.Circle
                ? AreaConverters.AreaStringToCoordinates(lines)
                : AreaConverters.AreaStringToMultiPolygon(lines);

            if (_uow.Geofences.Any(fence => fence.Name == name))
            {
                // Geofence already exists by name
                ModelState.AddModelError("Geofence", $"Geofence with name '{name}' already exists.");
                return View(geofence);
            }
            geofence = new Geofence
            {
                Name = name,
                Type = type,
                Data = new GeofenceData
                {
                    Area = area,
                },
            };

            // Add geofence to database
            await _uow.Geofences.AddAsync(geofence);
            await _uow.CommitAsync();

            // Add geofence to geofence service cache
            _geofenceService.Add(geofence);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError("Geofence", $"Unknown error occurred while creating new geofence.");

            var geofenceNames = (await _uow.Geofences.FindAllAsync()).Select(x => x.Name);
            ViewData["GeofenceNames"] = geofenceNames;
            ViewData["MapConfig"] = _mapConfig.ToJson();
            return View(geofence);
        }
    }

    // GET: GeofenceController/Edit/5
    public async Task<ActionResult> Edit(string id)
    {
        var geofenceNames = (await _uow.Geofences.FindAllAsync()).Select(x => x.Name);
        var geofence = await _uow.Geofences.FindByIdAsync(id);
        if (geofence == null)
        {
            // Failed to retrieve geofence from database, does it exist?
            ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
            return View(geofence);
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
        var geofence = await _uow.Geofences.FindByIdAsync(id);
        if (geofence == null)
        {
            // Failed to retrieve geofence from database, does it exist?
            ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
            return View(geofence);
        }

        try
        {
            var name = Convert.ToString(collection["Name"]);
            //var type = (GeofenceType)Convert.ToUInt16(collection["Type"]);
            var type = GeofenceType.StringToGeofenceType(collection["Type"].ToString());
            var data = Convert.ToString(collection["Data.Area"]);
            var lines = data.Replace("<br>", "\r\n").Replace("\r\n", "\n");

            dynamic area = type == GeofenceType.Circle
                ? AreaConverters.AreaStringToCoordinates(lines)
                : AreaConverters.AreaStringToMultiPolygon(lines);

            geofence.Name = name;
            geofence.Type = type;
            if (geofence.Data == null)
            {
                geofence.Data ??= new();
            }
            // NOTE: geofence.Data.Area = area;
            geofence.Data["area"] = area;

            // Update geofence in database
            await _uow.Geofences.UpdateAsync(geofence);
            await _uow.CommitAsync();

            _geofenceService.Edit(geofence, id);

            // Get list of instances that have geofence
            var instancesWithGeofence = await _uow.Instances.FindAsync(instance => instance.Geofences.Contains(geofence.Name));
            foreach (var instance in instancesWithGeofence)
            {
                // Reload instance so updated geofence is applied
                await _jobControllerService.ReloadInstanceAsync(instance, instance.Name);
            }

            return RedirectToAction(nameof(Index));
        }
        catch //(Exception ex)
        {
            ModelState.AddModelError("Geofence", $"Unknown error occurred while editing geofence '{id}'.");

            var geofenceNames = (await _uow.Geofences.FindAllAsync()).Select(x => x.Name);
            ViewData["GeofenceNames"] = geofenceNames;
            ViewData["MapConfig"] = _mapConfig.ToJson();
            return View(geofence);
        }
    }

    // GET: GeofenceController/Delete/5
    public async Task<ActionResult> Delete(string id)
    {
        var geofence = await _uow.Geofences.FindByIdAsync(id);
        if (geofence == null)
        {
            // Failed to retrieve geofence from database, does it exist?
            ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
            return View(geofence);
        }

        return View(geofence);
    }

    // POST: GeofenceController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(string id, IFormCollection collection)
    {
        var geofence = await _uow.Geofences.FindByIdAsync(id);
        if (geofence == null)
        {
            // Failed to retrieve geofence from database, does it exist?
            ModelState.AddModelError("Geofence", $"Geofence does not exist with id '{id}'.");
            return View(geofence);
        }

        try
        {
            // Delete geofence from database
            await _uow.Geofences.RemoveAsync(geofence);
            await _uow.CommitAsync();

            _geofenceService.Delete(id);

            // Get list of instances that have geofence
            var instancesWithGeofence = await _uow.Instances.FindAsync(instance => instance.Geofences.Contains(geofence.Name));
            foreach (var instance in instancesWithGeofence)
            {
                instance.Geofences.Remove(geofence.Name);

                // Reload instance so removed geofence is not ignored
                await _jobControllerService.ReloadInstanceAsync(instance, instance.Name);
            }

            return RedirectToAction(nameof(Index));
        }
        catch //(Exception ex)
        {
            ModelState.AddModelError("Geofence", $"Unknown error occurred while deleting geofence '{id}'.");
            return View(geofence);
        }
    }
}