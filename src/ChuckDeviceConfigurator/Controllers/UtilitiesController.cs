namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Configuration;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.JobControllers;

    [Authorize(Roles = RoleConsts.UtilitiesRole)]
    public class UtilitiesController : Controller
    {
        private readonly ILogger<UtilitiesController> _logger;
        private readonly ControllerDbContext _deviceContext;
        private readonly MapDbContext _mapContext;
        private readonly IJobControllerService _jobControllerService;
        private readonly LeafletMapConfig _mapConfig;

        public UtilitiesController(
            ILogger<UtilitiesController> logger,
            ControllerDbContext deviceContext,
            MapDbContext mapContext,
            IJobControllerService jobControllerService,
            IOptions<LeafletMapConfig> mapConfig)
        {
            _logger = logger;
            _deviceContext = deviceContext;
            _mapContext = mapContext;
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

                return RedirectToAction(nameof(ClearQuests));
            }
            catch
            {
                ModelState.AddModelError("Utilities", $"Unknown error occurred while clearing quests.");
                return View(model);
            }
        }

        #endregion

        #region Convert Forts

        // GET: UtilitiesController/ConvertForts
        public ActionResult ConvertForts()
        {
            // Retrieve Pokestops/Gyms that have been upgraded/downgraded
            var pokestops = _mapContext.Pokestops.ToList();
            var gyms = _mapContext.Gyms.ToList();
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
                var pokestops = _mapContext.Pokestops.ToList();
                var gyms = _mapContext.Gyms.ToList();
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
                var pokestop = await _mapContext.Pokestops.FindAsync(id);
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
                var pokestop = await _mapContext.Pokestops.FindAsync(id);
                if (pokestop != null)
                {
                    _mapContext.Pokestops.Remove(pokestop);
                    await _mapContext.SaveChangesAsync();
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
                var gym = await _mapContext.Gyms.FindAsync(id);
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
                var gym = await _mapContext.Gyms.FindAsync(id);
                if (gym != null)
                {
                    _mapContext.Gyms.Remove(gym);
                    await _mapContext.SaveChangesAsync();
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
                var pokestops = _mapContext.Pokestops.ToList();
                var gyms = _mapContext.Gyms.ToList();
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
                var pokestops = _mapContext.Pokestops.ToList();
                var gyms = _mapContext.Gyms.ToList();
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
        public ActionResult ClearStalePokestops()
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var pokestops = _mapContext.Pokestops
                .Where(pokestop => Math.Abs((decimal)now - pokestop.Updated) > Strings.OneDayS)
                .ToList();
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
                var pokestopsToDelete = _mapContext.Pokestops
                    .Where(pokestop => Math.Abs((decimal)now - pokestop.Updated) > Strings.OneDayS)
                    .ToList();

                await _mapContext.Pokestops.BulkDeleteAsync(pokestopsToDelete, options =>
                {
                    options.UseTableLock = true;
                });

                _logger.LogInformation($"Deleted {pokestopsToDelete.Count:N0} stale Pokestops from the database.");
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
        public ActionResult ReloadInstance()
        {
            ViewBag.Instances = _deviceContext.Instances.ToList();
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
                    ViewBag.Instances = _deviceContext.Instances.ToList();
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
                ViewBag.PokemonCount = _mapContext.Pokemon.LongCount().ToString("N0");
                ViewBag.IncidentsCount = _mapContext.Incidents.LongCount().ToString("N0");
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
                        count = _mapContext.Pokemon.Count(pokemon => Math.Abs((decimal)now - pokemon.ExpireTimestamp) >= time);
                        break;
                    case "Incidents":
                        count = _mapContext.Incidents.Count(incident => Math.Abs((decimal)now - incident.Expiration) >= time);
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
                        var pokemonCount = await _mapContext.Pokemon
                            .Where(pokemon => Math.Abs((decimal)now - pokemon.ExpireTimestamp) > time)
                            .DeleteFromQueryAsync(options =>
                            {
                                options.UseTableLock = true;
                            });
                        sw.Stop();
                        var pkmnSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
                        _logger.LogInformation($"Successfully deleted {pokemonCount:N0} old Pokemon from the database in {pkmnSeconds}s");
                        break;
                    case "Incidents":
                        sw.Start();
                        var invasionsCount = await _mapContext.Incidents
                            .Where(incident => Math.Abs((decimal)now - incident.Expiration) > time)
                            .DeleteFromQueryAsync(options =>
                            {
                                options.UseTableLock = true;
                            });
                        sw.Stop();
                        var invasionSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
                        _logger.LogInformation($"Successfully deleted {invasionsCount:N0} old Invasions from the database in {invasionSeconds}s");
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
        public ActionResult RouteGenerator()
        {
            var geofenceNames = _deviceContext.Geofences.ToList().Select(x => x.Name);
            ViewData["GeofenceNames"] = geofenceNames;
            ViewData["MapConfig"] = _mapConfig.ToJson();
            return View();
        }

        #endregion

        #region Private Methods

        private async Task<bool> ConvertPokestopToGymAsync(Pokestop pokestop)
        {
            var gym = await _mapContext.Gyms.FindAsync(pokestop.Id);
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
                    _mapContext.Pokestops.Remove(pokestop);

                    if (needsUpdate)
                    {
                        // Update Gym details
                        gym.IsEnabled = true;
                        gym.IsDeleted = false;
                        _mapContext.Gyms.Update(gym);
                    }
                    await _mapContext.SaveChangesAsync();
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
                await _mapContext.Gyms.AddAsync(gym);
                await _mapContext.SaveChangesAsync();
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
            var pokestop = await _mapContext.Pokestops.FindAsync(gym.Id);
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
                    _mapContext.Gyms.Remove(gym);

                    if (needsUpdate)
                    {
                        // Update Pokestop details
                        pokestop.IsEnabled = true;
                        pokestop.IsDeleted = false;
                        _mapContext.Pokestops.Update(pokestop);
                    }
                    await _mapContext.SaveChangesAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"An error occurred while trying to convert Gym ('{gym.Id}') to Pokestop: {ex}");
                    return false;
                }
            }

            // Delete old Pokestop
            _mapContext.Gyms.Remove(gym);

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
                await _mapContext.Pokestops.AddAsync(pokestop);
                await _mapContext.SaveChangesAsync();
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
}