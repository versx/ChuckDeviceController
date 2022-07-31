namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Extensions;
    using ChuckDeviceConfigurator.JobControllers;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Extensions;

    [Authorize(Roles = RoleConsts.UtilitiesRole)]
    public class UtilitiesController : Controller
    {
        private readonly ILogger<UtilitiesController> _logger;
        private readonly DeviceControllerContext _deviceContext;
        private readonly MapDataContext _mapContext;
        private readonly IJobControllerService _jobControllerService;

        public UtilitiesController(
            ILogger<UtilitiesController> logger,
            DeviceControllerContext deviceContext,
            MapDataContext mapContext,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _deviceContext = deviceContext;
            _mapContext = mapContext;
            _jobControllerService = jobControllerService;
        }

        // GET: UtilitiesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: UtilitiesController/ClearQuests
        public ActionResult ClearQuests()
        {
            var model = new ClearQuestsViewModel();
            var geofences = _deviceContext.Geofences.Where(geofence => geofence.Type == GeofenceType.Geofence)
                                                    .Select(geofence => geofence.Name)
                                                    .ToList();
            var instances = _deviceContext.Instances.Where(instance => instance.Type == InstanceType.AutoQuest)
                                                    .Select(instance => instance.Name)
                                                    .ToList();
            ViewBag.Geofences = geofences;
            ViewBag.Instances = instances;
            return View(model);
        }

        // POST: UtilitiesController/ClearQuests
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ClearQuests(ClearQuestsViewModel model)
        {
            try
            {
                if (!string.IsNullOrEmpty(model.InstanceName))
                {
                    // Get job controller instance
                    var jobController = _jobControllerService.GetInstanceControllerByName(model.InstanceName);
                    if (jobController is not AutoInstanceController questController)
                    {
                        ModelState.AddModelError("Utilities", $"Failed to find job controller instance with name '{model.InstanceName}'");
                        return View(model);
                    }

                    // Clear quest instance quests
                    await questController.ClearQuestsAsync();
                    _logger.LogInformation($"All quests have been cleared for instance '{model.InstanceName}'");
                }
                if (!string.IsNullOrEmpty(model.GeofenceName))
                {
                    // Retrieve geofence from database
                    var geofence = await _deviceContext.Geofences.FindAsync(model.GeofenceName);
                    if (geofence == null)
                    {
                        ModelState.AddModelError("Utilities", $"Failed to find geofence with name '{model.GeofenceName}'");
                        return View(model);
                    }

                    // Convert/parse geofence coordinates to multi polygon object
                    var (multiPolygons, _) = geofence.ConvertToMultiPolygons();
                    if ((multiPolygons?.Count ?? 0) == 0)
                    {
                        ModelState.AddModelError("Utilities", $"Failed to clear quests for geofence '{model.GeofenceName}', no multi polygons found");
                        return View(model);
                    }

                    // Clear quests by geofence
                    await _mapContext.ClearQuestsAsync(multiPolygons!);
                    _logger.LogInformation($"All quests have been cleared in geofence '{model.GeofenceName}'");
                }
                if (string.IsNullOrEmpty(model.InstanceName) && string.IsNullOrEmpty(model.GeofenceName))
                {
                    // Clear all quests
                    await _mapContext.ClearQuestsAsync();
                    _logger.LogInformation($"All quests have been cleared");
                }

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("Utilities", $"Unknown error occurred while clearing quests.");
                return View(model);
            }
        }
    }
}