namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Extensions;
    using ChuckDeviceConfigurator.JobControllers;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

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
            var convertiblePokestops = pokestops.Where(pokestop => gyms.Exists(gym => gym.Id == pokestop.Id && gym.Updated > pokestop.Updated))
                                                .ToList();
            convertiblePokestops.ForEach(pokestop => pokestop.UpdatedTime = pokestop.Updated.GetLastUpdatedStatus());
            var convertibleGyms = gyms.Where(gym => pokestops.Exists(pokestop => pokestop.Id == gym.Id && pokestop.Updated > gym.Updated))
                                      .ToList();
            convertibleGyms.ForEach(gym => gym.UpdatedTime = gym.Updated.GetLastUpdatedStatus());

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
                var convertiblePokestops = pokestops.Where(pokestop => gyms.Exists(gym => gym.Id == pokestop.Id && gym.Updated > pokestop.Updated))
                                                    .ToList();
                var convertibleGyms = gyms.Where(gym => pokestops.Exists(pokestop => pokestop.Id == gym.Id && pokestop.Updated > gym.Updated))
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
                var convertiblePokestops = pokestops.Where(pokestop => gyms.Exists(gym => gym.Id == pokestop.Id && gym.Updated > pokestop.Updated))
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
                var convertibleGyms = gyms.Where(gym => pokestops.Exists(pokestop => pokestop.Id == gym.Id && pokestop.Updated > gym.Updated))
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
            var pokestops = _mapContext.Pokestops.Where(pokestop => now - pokestop.Updated > Strings.OneDayS)
                                                 .ToList();

            pokestops.ForEach(pokestop => pokestop.UpdatedTime = pokestop.Updated.GetLastUpdatedStatus());
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
                var pokestopsToDelete = _mapContext.Pokestops.Where(pokestop => now - pokestop.Updated > Strings.OneDayS)
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

        // GET: UtilitiesController/TruncateData
        public ActionResult TruncateData()
        {
            // TODO: Maybe setup schedules to truncate at an interval
            // TODO: When a data type is selected, show the amount that'll be deleted based on the time span selected
            ViewBag.PokemonCount = _mapContext.Pokemon.LongCount().ToString("N0");
            ViewBag.IncidentsCount = _mapContext.Incidents.LongCount().ToString("N0");
            ViewBag.DataTypes = new List<string>
            {
                "Pokemon",
                "Incidents",
            };
            return View();
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
                        var pokemon = _mapContext.Pokemon.Where(pokemon => pokemon.ExpireTimestamp < now && now - pokemon.ExpireTimestamp > time).ToList();
                        var pokemonCount = pokemon.Count;
                        await _mapContext.Pokemon.BulkDeleteAsync(pokemon, options =>
                        {
                            options.UseTableLock = true;
                        });
                        sw.Stop();
                        var pkmnSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
                        _logger.LogInformation($"Successfully deleted {pokemonCount:N0} old Pokemon from the database in {pkmnSeconds}s");
                        break;
                    case "Incidents":
                        sw.Start();
                        var invasions = _mapContext.Incidents.Where(incident => incident.Expiration < now && now - incident.Expiration > time).ToList();
                        var invasionsCount = invasions.Count;
                        await _mapContext.Incidents.BulkDeleteAsync(invasions, options =>
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

        // POST: UtilitiesController/ReQuest
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

                // Delete old Pokestop
                _mapContext.Pokestops.Remove(pokestop);
                await _mapContext.SaveChangesAsync();

                if (needsUpdate)
                {
                    // Update Gym details
                    gym.IsEnabled = true;
                    gym.IsDeleted = false;
                    _mapContext.Gyms.Update(gym);
                    await _mapContext.SaveChangesAsync();
                }
                return true;
            }
            else
            {
                // TODO: Insert new gym from pokestop properties
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

                // Delete old Gym
                _mapContext.Gyms.Remove(gym);
                await _mapContext.SaveChangesAsync();

                if (needsUpdate)
                {
                    // Update Pokestop details
                    pokestop.IsEnabled = true;
                    pokestop.IsDeleted = false;
                    _mapContext.Pokestops.Update(pokestop);
                    await _mapContext.SaveChangesAsync();
                }
                return true;
            }
            else
            {
                // TODO: Insert new pokestop from gym properties
            }

            return false;
        }

        #endregion
    }
}