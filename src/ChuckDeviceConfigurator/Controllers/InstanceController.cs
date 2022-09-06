namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using static POGOProtos.Rpc.PokemonDisplayProto.Types;

    using ChuckDeviceConfigurator.JobControllers;
    using ChuckDeviceConfigurator.Localization;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.TimeZone;
    using ChuckDeviceConfigurator.Utilities;
    using ChuckDeviceConfigurator.ViewModels;
    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions.Json;

    [Authorize(Roles = RoleConsts.InstancesRole)]
    public class InstanceController : Controller
    {
        private readonly ILogger<InstanceController> _logger;
        private readonly ControllerDbContext _context;
        private readonly IJobControllerService _jobControllerService;
        private readonly ITimeZoneService _timeZoneService;

        public InstanceController(
            ILogger<InstanceController> logger,
            ControllerDbContext context,
            IJobControllerService jobControllerService,
            ITimeZoneService timeZoneService)
        {
            _logger = logger;
            _context = context;
            _jobControllerService = jobControllerService;
            _timeZoneService = timeZoneService;
        }

        // GET: InstanceController
        public async Task<ActionResult> Index(bool autoRefresh = false)
        {
            var instances = _context.Instances.ToList();
            var devices = _context.Devices.ToList();

            foreach (var instance in instances)
            {
                var devicesAssigned = devices.Where(device => device.InstanceName == instance.Name)
                                             .ToList();
                var devicesOnline = devicesAssigned.Count(device => Utils.IsDeviceOnline(device.LastSeen ?? 0));
                var devicesOffline = devicesAssigned.Count - devicesOnline;
                instance.DeviceCount = $"{devicesOnline}/{devicesAssigned.Count}|{devicesOffline}";
                var status = await _jobControllerService.GetStatusAsync(instance);
                instance.Status = status;
            }

            var model = new ViewModelsModel<Instance>
            {
                AutoRefresh = autoRefresh,
                Items = instances,
            };
            if (autoRefresh)
            {
                Response.Headers["Refresh"] = Strings.DefaultTableRefreshRateS;
            }
            ViewBag.AutoRefresh = autoRefresh;
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

            // Get devices assigned to instance
            var devicesAssigned = _context.Devices.Where(device => device.InstanceName == instance.Name)
                                                  .ToList();
            var devicesOnline = devicesAssigned.Count(device => Utils.IsDeviceOnline(device.LastSeen ?? 0));
            var devicesOffline = devicesAssigned.Count - devicesOnline;
            var status = await _jobControllerService.GetStatusAsync(instance);
            var model = new InstanceDetailsViewModel
            {
                Name = instance.Name,
                Type = instance.Type,
                MinimumLevel = instance.MinimumLevel,
                MaximumLevel = instance.MaximumLevel,
                Geofences = instance.Geofences,
                Data = instance.Data,
                DeviceCount = $"{devicesOnline}/{devicesAssigned.Count}|{devicesOffline}",
                Devices = devicesAssigned,
                Status = status,
            };

            return View(model);
        }

        // GET: InstanceController/Create
        public ActionResult Create()
        {
            // Create dummy instance model to provide default properties
            var model = new ManageInstanceViewModel
            {
                Type = null,
                MinimumLevel = Strings.DefaultMinimumLevel,
                MaximumLevel = Strings.DefaultMaximumLevel,
                Data = PopulateViewModelFromInstanceData(),
            };

            var geofences = _context.Geofences.ToList();
            ViewBag.CustomInstanceTypes = _jobControllerService.CustomInstanceTypes;
            ViewBag.Devices = _context.Devices.ToList();
            ViewBag.Geofences = geofences;
            ViewBag.GeofencesJson = geofences.ToJson();
            ViewBag.Instances = _context.Instances.ToList();
            ViewBag.IvLists = _context.IvLists.ToList();
            ViewBag.TimeZones = _timeZoneService.TimeZones.Select(pair => new { Name = pair.Key }).ToList();
            return View(model);
        }

        // POST: InstanceController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(ManageInstanceViewModel model)
        {
            try
            {
                if (_context.Instances.Any(inst => inst.Name == model.Name))
                {
                    // Instance exists already by name
                    ModelState.AddModelError("Instance", $"Instance with name '{model.Name}' already exists.");
                    return View();
                }

                var instance = new Instance
                {
                    Name = model.Name,
                    Type = model.Type ?? InstanceType.CirclePokemon,
                    MinimumLevel = model.MinimumLevel,
                    MaximumLevel = model.MaximumLevel,
                    Geofences = model.Geofences,
                    Data = PopulateInstanceDataFromModel(model.Data),
                };

                // Add instance to database
                await _context.Instances.AddAsync(instance);
                await _context.SaveChangesAsync();

                await _jobControllerService.AddInstanceAsync(instance);

                // Assign devices to instance and reload devices in cache
                if (model.AssignedDevices != null && model.AssignedDevices.Count > 0)
                {
                    await AssignDevicesToInstance(model.AssignedDevices, model.Name);
                }

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

            var assignedDevices = _context.Devices.Where(device => device.InstanceName == instance.Name)
                                                  .Select(device => device.Uuid)
                                                  .ToList();

            var model = new ManageInstanceViewModel
            {
                Name = instance.Name,
                Type = instance.Type,
                MinimumLevel = instance.MinimumLevel,
                MaximumLevel = instance.MaximumLevel,
                Geofences = instance.Geofences,
                Data = PopulateViewModelFromInstanceData(instance.Data),
                AssignedDevices = assignedDevices,
            };

            var geofences = _context.Geofences.ToList();
            ViewBag.CustomInstanceTypes = _jobControllerService.CustomInstanceTypes;
            ViewBag.Devices = _context.Devices.ToList();
            ViewBag.Geofences = geofences;
            ViewBag.GeofencesJson = geofences.ToJson();
            ViewBag.Instances = _context.Instances.ToList();
            ViewBag.IvLists = _context.IvLists.ToList();
            ViewBag.TimeZones = _timeZoneService.TimeZones.Select(pair => new { Name = pair.Key });
            return View(model);
        }

        // POST: InstanceController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, ManageInstanceViewModel model)
        {
            try
            {
                var instance = await _context.Instances.FindAsync(id);
                if (instance == null)
                {
                    // Failed to retrieve instance from database, does it exist?
                    ModelState.AddModelError("Instance", $"Instance does not exist with id '{id}'.");
                    return View(model);
                }

                // Check if new name already exists and not current instance
                if (_context.Instances.Any(inst => inst.Name == model.Name && inst.Name != id))
                {
                    ModelState.AddModelError("Instance", $"Instance with name '{model.Name}' already exists, please choose a different name.");
                    return View(model);
                }

                instance.Name = model.Name;
                instance.Type = model.Type ?? InstanceType.CirclePokemon;
                instance.MinimumLevel = model.MinimumLevel;
                instance.MaximumLevel = model.MaximumLevel;
                instance.Geofences = model.Geofences;
                instance.Data = PopulateInstanceDataFromModel(model.Data);

                // Update instance in database
                _context.Instances.Update(instance);
                await _context.SaveChangesAsync();

                // Reload instance cache
                await _jobControllerService.ReloadInstanceAsync(instance, id);

                // Assign devices to instance and reload devices in cache
                if (model.AssignedDevices != null && model.AssignedDevices.Count > 0)
                {
                    await AssignDevicesToInstance(model.AssignedDevices, id);
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch //(Exception ex)
            {
                /*
                 * The property 'Instance.Name' is part of a key and so cannot be modified or marked as modified.
                 * To change the principal of an existing entity with an identifying foreign key, first delete
                 * the dependent and invoke 'SaveChanges', and then associate the dependent with the new principal.
                 */
                ModelState.AddModelError("Instance", $"Unknown error occurred while editing instance '{id}'.");
                return View(model);
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
            instance.Status = await _jobControllerService.GetStatusAsync(instance);

            // Get devices assigned to instance
            var devicesAssigned = _context.Devices.Where(device => device.InstanceName == instance.Name)
                                                  .ToList();
            var devicesOnline = devicesAssigned.Count(device => Utils.IsDeviceOnline(device.LastSeen ?? 0));
            var devicesOffline = devicesAssigned.Count - devicesOnline;
            var status = await _jobControllerService.GetStatusAsync(instance);
            var model = new InstanceDetailsViewModel
            {
                Name = instance.Name,
                Type = instance.Type,
                MinimumLevel = instance.MinimumLevel,
                MaximumLevel = instance.MaximumLevel,
                Geofences = instance.Geofences,
                Data = instance.Data,
                DeviceCount = $"{devicesOnline}/{devicesAssigned.Count}|{devicesOffline}",
                Devices = devicesAssigned,
                Status = status,
            };

            ViewBag.Devices = devicesAssigned;
            return View(model);
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

        #region IV Queue Routes

        // GET: InstanceController/IvQueue/test
        [HttpGet("/Instance/IvQueue/{name}")]
        public ActionResult IvQueue(string name, bool autoRefresh = false)
        {
            try
            {
                var ivQueue = _jobControllerService.GetQueue<Pokemon>(name);
                var queueItems = ivQueue.Select(item =>
                {
                    var lat = Math.Round(item.Latitude, 5);
                    var lon = Math.Round(item.Longitude, 5);
                    var name = Translator.Instance.GetPokemonName(item.PokemonId);
                    var form = Translator.Instance.GetFormName(item.Form ?? 0);
                    var costume = Translator.Instance.GetCostumeName(item.Costume ?? 0);
                    return new IvQueueItemViewModel
                    {
                        EncounterId = item.Id,
                        PokemonId = item.PokemonId,
                        PokemonName = name,
                        PokemonForm = form,
                        PokemonFormId = item.Form ?? 0,
                        PokemonCostume = costume,
                        PokemonCostumeId = item.Costume ?? 0,
                        PokemonGender = (Gender)item.Gender,
                        Latitude = lat,
                        Longitude = lon,
                    };
                }).ToList();
                var model = new IvQueueViewModel
                {
                    Name = name,
                    Queue = queueItems,
                    AutoRefresh = autoRefresh,
                };
                if (autoRefresh)
                {
                    Response.Headers["Refresh"] = Strings.DefaultTableRefreshRateS;
                }
                return View(model);
            }
            catch
            {
                _logger.LogError($"Unknown error occurred while retrieving IV queue '{name}'.");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: InstanceController/IvQueue/test/Remove/5
        [HttpGet("/Instance/IvQueue/{name}/Remove/{id}")]
        public ActionResult IvQueueRemove(string name, string id)
        {
            try
            {
                // Remove Pokemon with index from IV queue list by name
                _jobControllerService.RemoveFromQueue(name, id);
            }
            catch
            {
                _logger.LogError($"Unknown error occurred while removing Pokemon encounter ({id}) from IV queue '{name}'.");
            }
            return RedirectToAction(nameof(IvQueue), new { name });
        }

        // GET: InstanceController/IvQueue/test/Clear
        [HttpGet("/Instance/IvQueue/{name}/Clear")]
        public ActionResult ClearQueue(string name)
        {
            try
            {
                // Clear all pending Pokemon encounters from the specified IV queue
                _jobControllerService.ClearQueue(name);
            }
            catch
            {
                _logger.LogError($"Unknown error occurred while clearing IV queue '{name}'.");
            }
            return RedirectToAction(nameof(IvQueue), new { name });
        }

        #endregion

        #region Quest Queue Routes

        // GET: InstanceController/QuestQueue/test
        [HttpGet("/Instance/QuestQueue/{name}")]
        public ActionResult QuestQueue(string name, bool autoRefresh = false)
        {
            try
            {
                var questQueue = _jobControllerService.GetQueue<PokestopWithMode>(name);
                var queueItems = questQueue.Select(item =>
                {
                    var pokestop = item.Pokestop!;
                    var lat = Math.Round(pokestop.Latitude, 5);
                    var lon = Math.Round(pokestop.Longitude, 5);
                    return new QuestQueueItemViewModel
                    {
                        Id = pokestop.Id,
                        Name = pokestop.Name,
                        Image = $"<img src='{pokestop.Url}' height='48' width='48' />",
                        IsAlternative = item.IsAlternative,
                        Latitude = lat,
                        Longitude = lon,
                    };
                }).ToList();
                var model = new QuestQueueViewModel
                {
                    Name = name,
                    Queue = queueItems,
                    AutoRefresh = autoRefresh,
                };
                if (autoRefresh)
                {
                    Response.Headers["Refresh"] = Strings.DefaultTableRefreshRateS;
                }
                return View(model);
            }
            catch
            {
                _logger.LogError($"Unknown error occurred while retrieving quest queue '{name}'.");
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: InstanceController/QuestQueue/test/Remove/5
        [HttpGet("/Instance/QuestQueue/{name}/Remove/{id}")]
        public ActionResult QuestQueueRemove(string name, string id)
        {
            try
            {
                // Remove Pokestop from Quest queue list by name
                _jobControllerService.RemoveFromQueue(name, id);
            }
            catch
            {
                _logger.LogError($"Unknown error occurred while removing Pokestop ({id}) from Quest queue '{name}'.");
            }
            return RedirectToAction(nameof(QuestQueue), new { name });
        }

        // GET: InstanceController/QuestQueue/test/Clear
        [HttpGet("/Instance/QuestQueue/{name}/Clear")]
        public ActionResult ClearQuestQueue(string name)
        {
            try
            {
                // Clear all pending Pokestop quests from the specified Quest queue
                _jobControllerService.ClearQueue(name);
            }
            catch
            {
                _logger.LogError($"Unknown error occurred while clearing Quest queue '{name}'.");
            }
            return RedirectToAction(nameof(QuestQueue), new { name });
        }

        #endregion

        #region Private Methods

        private async Task AssignDevicesToInstance(List<string> deviceUuids, string instanceName)
        {
            foreach (var deviceUuid in deviceUuids)
            {
                // Retrieve device by uuid from database
                var device = await _context.Devices.FindAsync(deviceUuid);
                if (device == null)
                {
                    _logger.LogWarning($"Failed to retrieve device from database with UUID '{deviceUuid}', unable to assign device to instance '{instanceName}'");
                    continue;
                }

                // Check if device is already assigned to instance
                if (device.InstanceName == instanceName)
                {
                    // Device is already assigned to instance, skip...
                    continue;
                }

                // Assign device to new instance
                device.InstanceName = instanceName;
                _context.Devices.Update(device);
                await _context.SaveChangesAsync();

                // Reload device cache
                _jobControllerService.ReloadDevice(device, deviceUuid);
            }
        }

        private static InstanceData PopulateInstanceDataFromModel(ManageInstanceDataViewModel model)
        {
            var instanceData = new InstanceData
            {
                // All
                AccountGroup = model?.AccountGroup ?? Strings.DefaultAccountGroup,
                IsEvent = model?.IsEvent ?? Strings.DefaultIsEvent,

                // Circle
                CircleRouteType = model?.CircleRouteType ?? Strings.DefaultCircleRouteType,
                EnableLureEncounters = model?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters,

                // Dynamic
                OptimizeDynamicRoute = model?.OptimizeDynamicRoute ?? Strings.DefaultOptimizeDynamicRoute,

                // Bootstrap
                FastBootstrapMode = model?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode,
                CircleSize = model?.CircleSize ?? Strings.DefaultCircleSize,
                OptimizeBootstrapRoute = model?.OptimizeBootstrapRoute ?? Strings.DefaultOptimizeBootstrapRoute,
                BootstrapCompleteInstanceName = model?.BootstrapCompleteInstanceName ?? Strings.DefaultBootstrapCompleteInstanceName,

                // IV
                IvList = model?.IvList ?? Strings.DefaultIvList,
                IvQueueLimit = model?.IvQueueLimit ?? Strings.DefaultIvQueueLimit,

                // Quests
                QuestMode = model?.QuestMode ?? Strings.DefaultQuestMode,
                TimeZone = model?.TimeZone ?? Strings.DefaultTimeZone,
                EnableDst = model?.EnableDst ?? Strings.DefaultEnableDst,
                SpinLimit = model?.SpinLimit ?? Strings.DefaultSpinLimit,
                UseWarningAccounts = model?.UseWarningAccounts ?? Strings.DefaultUseWarningAccounts,
                IgnoreS2CellBootstrap = model?.IgnoreS2CellBootstrap ?? Strings.DefaultIgnoreS2CellBootstrap,
                LogoutDelay = model?.LogoutDelay ?? Strings.DefaultLogoutDelay,
                MaximumSpinAttempts = model?.MaximumSpinAttempts ?? Strings.DefaultMaximumSpinAttempts,

                // Spawnpoint
                OnlyUnknownSpawnpoints = model?.OnlyUnknownSpawnpoints ?? Strings.DefaultOnlyUnknownSpawnpoints,
                OptimizeSpawnpointsRoute = model?.OptimizeSpawnpointsRoute ?? Strings.DefaultOptimizeSpawnpointRoute,

                // Leveling
                LevelingRadius = model?.LevelingRadius ?? Strings.DefaultLevelingRadius,
                StoreLevelingData = model?.StoreLevelingData ?? Strings.DefaultStoreLevelingData,
                StartingCoordinate = model?.StartingCoordinate ?? Strings.DefaultStartingCoordinate,

                CustomInstanceType = model?.CustomInstanceType ?? Strings.DefaultCustomInstanceType,
            };
            return instanceData;
        }

        private static ManageInstanceDataViewModel PopulateViewModelFromInstanceData(InstanceData? data = null)
        {
            var instanceDataModel = new ManageInstanceDataViewModel
            {
                // All
                AccountGroup = data?.AccountGroup ?? Strings.DefaultAccountGroup,
                IsEvent = data?.IsEvent ?? Strings.DefaultIsEvent,

                // Circle
                CircleRouteType = data?.CircleRouteType ?? Strings.DefaultCircleRouteType,
                EnableLureEncounters = data?.EnableLureEncounters ?? Strings.DefaultEnableLureEncounters,

                // Dynamic
                OptimizeDynamicRoute = data?.OptimizeDynamicRoute ?? Strings.DefaultOptimizeDynamicRoute,

                // Bootstrap
                FastBootstrapMode = data?.FastBootstrapMode ?? Strings.DefaultFastBootstrapMode,
                CircleSize = data?.CircleSize ?? Strings.DefaultCircleSize,
                OptimizeBootstrapRoute = data?.OptimizeBootstrapRoute ?? Strings.DefaultOptimizeBootstrapRoute,
                BootstrapCompleteInstanceName = data?.BootstrapCompleteInstanceName ?? Strings.DefaultBootstrapCompleteInstanceName,

                // IV
                IvList = data?.IvList ?? Strings.DefaultIvList,
                IvQueueLimit = data?.IvQueueLimit ?? Strings.DefaultIvQueueLimit,

                // Quests
                QuestMode = data?.QuestMode ?? Strings.DefaultQuestMode,
                SpinLimit = data?.SpinLimit ?? Strings.DefaultSpinLimit,
                EnableDst = data?.EnableDst ?? Strings.DefaultEnableDst,
                TimeZone = data?.TimeZone ?? Strings.DefaultTimeZone,
                LogoutDelay = data?.LogoutDelay ?? Strings.DefaultLogoutDelay,
                MaximumSpinAttempts = data?.MaximumSpinAttempts ?? Strings.DefaultMaximumSpinAttempts,
                IgnoreS2CellBootstrap = data?.IgnoreS2CellBootstrap ?? Strings.DefaultIgnoreS2CellBootstrap,
                UseWarningAccounts = data?.UseWarningAccounts ?? Strings.DefaultUseWarningAccounts,

                // Spawnpoints
                OnlyUnknownSpawnpoints = data?.OnlyUnknownSpawnpoints ?? Strings.DefaultOnlyUnknownSpawnpoints,
                OptimizeSpawnpointsRoute = data?.OptimizeSpawnpointsRoute ?? Strings.DefaultOptimizeSpawnpointRoute,

                // Leveling
                LevelingRadius = data?.LevelingRadius ?? Strings.DefaultLevelingRadius,
                StoreLevelingData = data?.StoreLevelingData ?? Strings.DefaultStoreLevelingData,
                StartingCoordinate = data?.StartingCoordinate ?? Strings.DefaultStartingCoordinate,

                // Custom
                CustomInstanceType = data?.CustomInstanceType ?? Strings.DefaultCustomInstanceType,
            };
            return instanceDataModel;
        }

        #endregion
    }
}