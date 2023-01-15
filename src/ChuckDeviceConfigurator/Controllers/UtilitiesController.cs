namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using ChuckDeviceConfigurator.Services.Jobs;
using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Common;
using ChuckDeviceController.Configuration;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.JobControllers;

[Authorize(Roles = RoleConsts.UtilitiesRole)]
public class UtilitiesController : BaseMvcController
{
    private readonly ILogger<UtilitiesController> _logger;
    private readonly IDapperUnitOfWork _uow;
    private readonly IJobControllerService _jobControllerService;
    private readonly LeafletMapConfig _mapConfig;

    public UtilitiesController(
        ILogger<UtilitiesController> logger,
        IDapperUnitOfWork uow,
        IJobControllerService jobControllerService,
        IOptions<LeafletMapConfig> mapConfig)
    {
        _logger = logger;
        _uow = uow;
        _jobControllerService = jobControllerService;
        _mapConfig = mapConfig.Value;
    }

    // GET: UtilitiesController
    public ActionResult Index()
    {
        return View();
    }

    #region Clear Quests

    // GET: UtilitiesController/ClearQuests
    public async Task<ActionResult> ClearQuests()
    {
        var model = new ClearQuestsViewModel();
        var geofences = await _uow.Geofences.FindAsync(geofence => geofence.Type == GeofenceType.Geofence);
        var geofenceNames = geofences.Select(geofence => geofence.Name);
        var instances = await _uow.Instances.FindAsync(instance => instance.Type == InstanceType.AutoQuest);
        var instanceNames = instances.Select(instance => instance.Name);
        ViewBag.Geofences = geofenceNames;
        ViewBag.Instances = instanceNames;
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
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Failed to find job controller instance with name '{model.InstanceName}'!",
                        Icon = NotificationIcon.Error,
                    });
                    return View(model);
                }

                // Clear quest instance quests
                await questController.ClearQuestsAsync();
                _logger.LogInformation($"All quests have been cleared for instance '{model.InstanceName}'");

                CreateNotification(new NotificationViewModel
                {
                    Message = $"All Pokestop quests for instance '{model.InstanceName}' have been cleared!",
                    Icon = NotificationIcon.Success,
                });
            }

            if (!string.IsNullOrEmpty(model.GeofenceName))
            {
                // Retrieve geofence from database
                var geofence = await _uow.Geofences.FindAsync(model.GeofenceName);
                if (geofence == null)
                {
                    ModelState.AddModelError("Utilities", $"Failed to find geofence with name '{model.GeofenceName}'");
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Failed to find geofence with name '{model.GeofenceName}'!",
                        Icon = NotificationIcon.Error,
                    });
                    return View(model);
                }

                // Convert/parse geofence coordinates to multi polygon object
                var (multiPolygons, _) = geofence.ConvertToMultiPolygons();
                if ((multiPolygons?.Count ?? 0) == 0)
                {
                    ModelState.AddModelError("Utilities", $"Failed to clear quests for geofence '{model.GeofenceName}', no multi-polygons found");
                    CreateNotification(new NotificationViewModel
                    {
                        Message = $"Failed to clear quests for geofence '{model.GeofenceName}', no multi-polygons found!",
                        Icon = NotificationIcon.Error,
                    });
                    return View(model);
                }

                // Clear quests by geofence
                await _uow.ClearQuestsAsync(multiPolygons!);
                _logger.LogInformation($"All quests have been cleared in geofence '{model.GeofenceName}'");

                CreateNotification(new NotificationViewModel
                {
                    Message = $"All Pokestop quests in geofence '{model.GeofenceName}' have been cleared!",
                    Icon = NotificationIcon.Success,
                });
            }

            if (string.IsNullOrEmpty(model.InstanceName) && string.IsNullOrEmpty(model.GeofenceName))
            {
                // Clear all quests
                await _uow.ClearQuestsAsync();
                _logger.LogInformation($"All quests have been cleared");

                CreateNotification(new NotificationViewModel
                {
                    Message = $"All Pokestop quests have been cleared!",
                    Icon = NotificationIcon.Success,
                });
            }

            return RedirectToAction(nameof(ClearQuests));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while clearing Pokestop quests.");
            CreateNotification(new NotificationViewModel
            {
                Message = $"Unknown error occurred while clearing Pokestop quests!",
                Icon = NotificationIcon.Error,
            });
            return View(model);
        }
    }

    #endregion

    #region Convert Forts

    // GET: UtilitiesController/ConvertForts
    public async Task<ActionResult> ConvertForts()
    {
        // Retrieve Pokestops/Gyms that have been upgraded/downgraded
        var pokestops = (await _uow.Pokestops.FindAllAsync()).ToList();
        var gyms = (await _uow.Gyms.FindAllAsync()).ToList();
        var convertiblePokestops = pokestops
            .Where(pokestop => gyms.Exists(gym => gym.Id == pokestop.Id && gym.Updated > pokestop.Updated))
            .ToList();
        var convertibleGyms = gyms
            .Where(gym => pokestops.Exists(pokestop => pokestop.Id == gym.Id && pokestop.Updated > gym.Updated))
            .ToList();

        var model = new ConvertFortsViewModel
        {
            PokestopsToGyms = convertiblePokestops,
            GymsToPokestops = convertibleGyms,
        };
        return View(model);
    }

    // POST: UtilitiesController/ConvertForts
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ConvertForts(ConvertFortsViewModel model)
    {
        try
        {
            var pokestops = (await _uow.Pokestops.FindAllAsync()).ToList();
            var gyms = (await _uow.Gyms.FindAllAsync()).ToList();
            var convertiblePokestops = pokestops
                .Where(pokestop => gyms.Exists(gym => gym.Id == pokestop.Id && gym.Updated > pokestop.Updated))
                .ToList();
            var convertibleGyms = gyms
                .Where(gym => pokestops.Exists(pokestop => pokestop.Id == gym.Id && pokestop.Updated > gym.Updated))
                .ToList();

            foreach (var pokestop in convertiblePokestops)
            {
                var result = await ConvertPokestopToGymAsync(pokestop);
                if (!result)
                {
                    _logger.LogError($"Failed to convert Pokestop to Gym with id '{pokestop.Id}'");
                }
            }

            foreach (var gym in convertibleGyms)
            {
                var result = await ConvertGymToPokestopAsync(gym);
                if (!result)
                {
                    _logger.LogError($"Failed to convert Gym to Pokestop with id '{gym.Id}'");
                }
            }

            return RedirectToAction(nameof(ConvertForts));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while converting forts.");
            return View(model);
        }
    }

    // GET: UtilitiesController/ConvertPokestop/3243243242
    [HttpGet]
    public async Task<ActionResult> ConvertPokestop(string id)
    {
        try
        {
            // Convert individual Pokestop to Gym
            var pokestop = await _uow.Pokestops.FindAsync(id);
            if (pokestop == null)
            {
                ModelState.AddModelError("Utilities", $"Failed to retrieve Pokestop with id '{id}'");
                return View();
            }

            var result = await ConvertPokestopToGymAsync(pokestop);
            if (!result)
            {
                _logger.LogError($"Failed to convert Pokestop to Gym with id '{id}'");
            }
            else
            {
                _logger.LogInformation($"Successfully converted Pokestop '{id}' to Gym");
            }

            return RedirectToAction(nameof(ConvertForts));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while converting Pokestop '{id}' to Gym.");
            return View();
        }
    }

    // GET: UtilitiesController/DeletePokestop/3243243242
    [HttpGet]
    public async Task<ActionResult> DeletePokestop(string id)
    {
        try
        {
            var pokestop = await _uow.Pokestops.FindAsync(id);
            if (pokestop != null)
            {
                var result = await _uow.Pokestops.DeleteAsync(pokestop.Id);
                if (!result)
                {
                    _logger.LogError("Failed to delete pokestop with id '{Id}'", id);
                }
            }
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while deleting Pokestop '{id}'.");
        }
        return RedirectToAction(nameof(ConvertForts));
    }

    // GET: UtilitiesController/ConvertGym/3243243242
    [HttpGet]
    public async Task<ActionResult> ConvertGym(string id)
    {
        try
        {
            // Convert individual Gym to Pokestop
            var gym = await _uow.Gyms.FindAsync(id);
            if (gym == null)
            {
                ModelState.AddModelError("Utilities", $"Failed to retrieve Gym with id '{id}'");
                return View();
            }

            var result = await ConvertGymToPokestopAsync(gym);
            if (!result)
            {
                _logger.LogError($"Failed to convert Gym to Pokestop with id '{id}'");
            }
            else
            {
                _logger.LogInformation($"Successfully converted Gym '{id}' to Pokestop");
            }

            return RedirectToAction(nameof(ConvertForts));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while converting Gym '{id}' to Pokestop.");
            return View();
        }
    }

    // GET: UtilitiesController/DeleteGym/3243243242
    [HttpGet]
    public async Task<ActionResult> DeleteGym(string id)
    {
        try
        {
            var gym = await _uow.Gyms.FindAsync(id);
            if (gym != null)
            {
                var result = await _uow.Gyms.DeleteAsync(gym.Id);
                if (!result)
                {
                    _logger.LogError("Failed to delete gym with id '{Id}'", id);
                }
            }
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while deleting Gym '{id}'.");
        }
        return RedirectToAction(nameof(ConvertForts));
    }

    // GET: UtilitiesController/ConvertPokestops
    [HttpGet]
    public async Task<ActionResult> ConvertPokestops(ConvertFortsViewModel model)
    {
        try
        {
            // Convert all Pokestops to Gyms
            var pokestops = (await _uow.Pokestops.FindAllAsync()).ToList();
            var gyms = (await _uow.Gyms.FindAllAsync()).ToList();
            var convertiblePokestops = pokestops
                .Where(pokestop => gyms.Exists(gym => gym.Id == pokestop.Id && gym.Updated > pokestop.Updated))
                .ToList();

            foreach (var pokestop in convertiblePokestops)
            {
                var result = await ConvertPokestopToGymAsync(pokestop);
                if (!result)
                {
                    _logger.LogError($"Failed to convert Pokestop to Gym with id '{pokestop.Id}'");
                }
            }

            return RedirectToAction(nameof(ConvertForts));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while converting Pokestops to Gyms.");
            return View(model);
        }
    }

    // GET: UtilitiesController/ConvertGyms
    [HttpGet]
    public async Task<ActionResult> ConvertGyms(ConvertFortsViewModel model)
    {
        try
        {
            // Convert all Gyms to Pokestops
            var pokestops = (await _uow.Pokestops.FindAllAsync()).ToList();
            var gyms = (await _uow.Gyms.FindAllAsync()).ToList();
            var convertibleGyms = gyms
                .Where(gym => pokestops.Exists(pokestop => pokestop.Id == gym.Id && pokestop.Updated > gym.Updated))
                .ToList();

            foreach (var gym in convertibleGyms)
            {
                var result = await ConvertGymToPokestopAsync(gym);
                if (!result)
                {
                    _logger.LogError($"Failed to convert Gym to Pokestop with id '{gym.Id}'");
                }
            }

            return RedirectToAction(nameof(ConvertForts));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while converting Gyms to Pokestops.");
            return View(model);
        }
    }

    #endregion

    #region Clear Stale Pokestops

    // GET: UtilitiesController/ClearStalePokestops
    public async Task<ActionResult> ClearStalePokestops()
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var pokestops = await _uow.Pokestops.FindAsync(pokestop => Math.Abs((decimal)now - pokestop.Updated) > Strings.OneDayS);
        return View(pokestops);
    }

    // POST: UtilitiesController/ClearStalePokestops
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ClearStalePokestops(List<Pokestop> pokestops)
    {
        try
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var pokestopsToDelete = await _uow.Pokestops.FindAsync(pokestop => Math.Abs((decimal)now - pokestop.Updated) > Strings.OneDayS);

            var pokestopIdsToDelete = pokestopsToDelete.Select(pokestop => pokestop.Id);
            var result = await _uow.Pokestops.DeleteRangeAsync(pokestopIdsToDelete);
            if (!result)
            {
                // Failed to delete stale pokestops
                _logger.LogError("Failed to delete stale pokestops.");
            }
            else
            {
                _logger.LogInformation($"Deleted {pokestopsToDelete.Count():N0} stale Pokestops from the database.");
            }
            return RedirectToAction(nameof(ClearStalePokestops));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while clearing all stale Pokestops.");
            return View(pokestops);
        }
    }

    #endregion

    #region Reload Instance

    // GET: UtilitiesController/ReloadInstance
    public async Task<ActionResult> ReloadInstance()
    {
        var instances = await _uow.Instances.FindAllAsync();
        ViewBag.Instances = instances;
        return View();
    }

    // POST: UtilitiesController/ReloadInstance
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> ReloadInstance(string instanceName)
    {
        try
        {
            var name = instanceName;
            var jobController = _jobControllerService.GetInstanceControllerByName(name);
            if (jobController == null)
            {
                ModelState.AddModelError("Utilities", $"Failed to find job controller instance with name '{name}'");
                var instances = await _uow.Instances.FindAllAsync();
                ViewBag.Instances = instances;
                return View(instanceName);
            }

            await jobController.ReloadAsync();

            _logger.LogInformation($"Job controller instance {name} reloaded.");
            return RedirectToAction(nameof(ReloadInstance));
        }
        catch
        {
            ModelState.AddModelError("Utilities", $"Unknown error occurred while reload instance.");
            return View(instanceName);
        }
    }

    #endregion

    #region Truncate Data

    // GET: UtilitiesController/TruncateData[?timeSpan=1&dataType=Pokemon]
    public ActionResult TruncateData(int? timeSpan = null, string? dataType = null)
    {
        if (timeSpan == null && dataType == null)
        {
            ViewBag.PokemonCount = _uow.Pokemon.Count().ToString("N0");
            ViewBag.IncidentsCount = _uow.Incidents.Count().ToString("N0");
            ViewBag.DataTypes = new List<string>
            {
                "Pokemon",
                "Incidents",
            };
            return View();
        }

        try
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var time = Convert.ToUInt64(timeSpan * Strings.SixtyMinutesS);
            var count = 0;
            switch (dataType)
            {
                case "Pokemon":
                    count = _uow.Pokemon.Count(pokemon => Math.Abs((decimal)now - pokemon.ExpireTimestamp) >= time);
                    break;
                case "Incidents":
                    count = _uow.Incidents.Count(incident => Math.Abs((decimal)now - incident.Expiration) >= time);
                    break;
                default:
                    _logger.LogWarning($"Unknown data type provided '{dataType}', unable to truncate.");
                    break;
            }
            return new JsonResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex}");
        }
        return new JsonResult(0);
    }

    // POST: UtilitiesController/TruncateData
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> TruncateData(int timeSpan, string dataType)
    {
        try
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var time = Convert.ToUInt64(timeSpan * Strings.SixtyMinutesS);
            var sw = new System.Diagnostics.Stopwatch();
            switch (dataType)
            {
                case "Pokemon":
                    sw.Start();
                    var pokemonToDelete = await _uow.Pokemon.FindAsync(pokemon => Math.Abs((decimal)now - pokemon.ExpireTimestamp) > time);
                    var pokemonIdsToDelete = pokemonToDelete.Select(p => p.Id);
                    var resultPokemon = await _uow.Pokemon.DeleteRangeAsync(pokemonIdsToDelete);
                    if (!resultPokemon)
                    {
                        // Failed
                    }
                    
                    sw.Stop();
                    var pkmnSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
                    _logger.LogInformation($"Successfully deleted {pokemonToDelete.Count():N0} old Pokemon from the database in {pkmnSeconds}s");
                    break;
                case "Incidents":
                    sw.Start();
                    var incidentsToDelete = await _uow.Incidents.FindAsync(incident => Math.Abs((decimal)now - incident.Expiration) > time);
                    var incidentIdsToDelete = incidentsToDelete.Select(i => i.Id);
                    var resultIncidents = await _uow.Incidents.DeleteRangeAsync(incidentIdsToDelete);
                    if (!resultIncidents)
                    {
                        // Failed
                    }

                    sw.Stop();
                    var invasionSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
                    _logger.LogInformation($"Successfully deleted {incidentsToDelete.Count():N0} old Invasions from the database in {invasionSeconds}s");
                    break;
                default:
                    _logger.LogWarning($"Unknown data type provided '{dataType}', unable to truncate.");
                    break;
            }
            return RedirectToAction(nameof(TruncateData));
        }
        catch (Exception ex)
        {
            ViewBag.DataTypes = new List<string>
            {
                "Pokemon",
                "Incidents",
            };
            ModelState.AddModelError("Utilities", $"Unknown error occurred while truncating data: {ex}.");
            return View();
        }
    }

    #endregion

    #region ReQuest

    // GET: UtilitiesController/ReQuest
    public ActionResult ReQuest()
    {
        return View();
    }

    // POST: UtilitiesController/ReQuest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ReQuest(string instanceName)
    {
        return View();
    }

    #endregion

    #region Route Generator

    // GET: UtilitiesController/RouteGenerator
    public async Task<ActionResult> RouteGenerator()
    {
        var geofences = await _uow.Geofences.FindAllAsync();;
        var geofenceNames = geofences.Select(geofence => geofence.Name);
        ViewData["GeofenceNames"] = geofenceNames;
        ViewData["MapConfig"] = _mapConfig.ToJson();
        return View();
    }

    #endregion

    #region Private Methods

    private async Task<bool> ConvertPokestopToGymAsync(Pokestop pokestop)
    {
        var gym = await _uow.Gyms.FindAsync(pokestop.Id);
        if (gym != null)
        {
            var needsUpdate = false;
            if (string.IsNullOrEmpty(gym.Name))
            {
                gym.Name = pokestop.Name;
                needsUpdate = true;
            }
            if (string.IsNullOrEmpty(gym.Url))
            {
                gym.Url = pokestop.Url;
                needsUpdate = true;
            }

            try
            {
                // Delete old Pokestop
                await _uow.Pokestops.DeleteAsync(pokestop.Id);

                if (needsUpdate)
                {
                    // Update Gym details
                    gym.IsEnabled = true;
                    gym.IsDeleted = false;
                    await _uow.Gyms.UpdateAsync(gym);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while trying to convert Pokestop ('{pokestop.Id}') to Gym: {ex}");
                return false;
            }
        }

        // Insert new gym with pokestop properties
        gym = new Gym
        {
            Id = pokestop.Id,
            Name = pokestop.Name,
            Url = pokestop.Url,
            Latitude = pokestop.Latitude,
            Longitude = pokestop.Longitude,
            IsArScanEligible = pokestop.IsArScanEligible,
            IsEnabled = true,
            SponsorId = pokestop.SponsorId,
            PowerUpEndTimestamp = pokestop.PowerUpEndTimestamp,
            PowerUpLevel = pokestop.PowerUpLevel,
            PowerUpPoints = pokestop.PowerUpPoints,
            FirstSeenTimestamp = pokestop.FirstSeenTimestamp,
            LastModifiedTimestamp = pokestop.LastModifiedTimestamp,
            Updated = pokestop.Updated,
            CellId = pokestop.CellId,
        };

        try
        {
            await _uow.Gyms.InsertAsync(gym);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while trying to convert Pokestop ('{pokestop.Id}') to Gym: {ex}");
        }
        return false;
    }

    private async Task<bool> ConvertGymToPokestopAsync(Gym gym)
    {
        var pokestop = await _uow.Pokestops.FindAsync(gym.Id);
        if (pokestop != null)
        {
            var needsUpdate = false;
            if (string.IsNullOrEmpty(pokestop.Name))
            {
                pokestop.Name = gym.Name;
                needsUpdate = true;
            }
            if (string.IsNullOrEmpty(pokestop.Url))
            {
                pokestop.Url = gym.Url;
                needsUpdate = true;
            }

            try
            {
                // Delete old Gym
                var result = await _uow.Gyms.DeleteAsync(gym.Id);
                if (!result)
                {
                    // Failed
                }

                if (needsUpdate)
                {
                    // Update Pokestop details
                    pokestop.IsEnabled = true;
                    pokestop.IsDeleted = false;
                    await _uow.Pokestops.UpdateAsync(pokestop);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while trying to convert Gym ('{gym.Id}') to Pokestop: {ex}");
                return false;
            }
        }

        // Delete old Pokestop
        var gymResult = await _uow.Gyms.DeleteAsync(gym.Id);
        if (!gymResult)
        {
            // Failed
        }

        // Insert new pokestop with gym properties
        pokestop = new Pokestop
        {
            Id = gym.Id,
            Name = gym.Name,
            Url = gym.Url,
            Latitude = gym.Latitude,
            Longitude = gym.Longitude,
            IsArScanEligible = gym.IsArScanEligible ?? false,
            IsEnabled = true,
            SponsorId = gym.SponsorId,
            PowerUpEndTimestamp = gym.PowerUpEndTimestamp,
            PowerUpLevel = gym.PowerUpLevel,
            PowerUpPoints = gym.PowerUpPoints,
            FirstSeenTimestamp = gym.FirstSeenTimestamp,
            LastModifiedTimestamp = gym.LastModifiedTimestamp,
            Updated = gym.Updated,
            CellId = gym.CellId,
        };

        try
        {
            await _uow.Pokestops.InsertAsync(pokestop);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while trying to convert Gym ('{gym.Id}') to Pokestop: {ex}");
        }
        return false;
    }

    #endregion
}