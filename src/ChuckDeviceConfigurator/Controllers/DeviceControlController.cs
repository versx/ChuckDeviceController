namespace ChuckDeviceConfigurator.Controllers
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Common.Cache;
    using ChuckDeviceController.Common.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Net.Models.Responses;

    [ApiController]
    public class DeviceControlController : ControllerBase
    {
        #region Variables

        private readonly ILogger<DeviceControlController> _logger;
        private readonly ControllerDbContext _context;
        private readonly IJobControllerService _jobControllerService;
        private readonly IMemoryCacheHostedService _memCache;

        #endregion

        #region Constructor

        public DeviceControlController(
            ILogger<DeviceControlController> logger,
            ControllerDbContext context,
            IJobControllerService jobControllerService,
            IMemoryCacheHostedService memCache)
        {
            _logger = logger;
            _context = context;
            _jobControllerService = jobControllerService;
            _memCache = memCache;
        }

        #endregion

        #region Routes

        [
            Route("/controler"),
            Route("/controller"),
            HttpGet,
        ]
        public string GetAsync() => ":)";

        [
            Route("/controler"),
            Route("/controller"),
            HttpPost,
        ]
        public async Task<DeviceResponse> PostAsync(DevicePayload payload)
        {
            var response = await HandleControllerRequestAsync(payload);
            return response;
        }

        #endregion

        private async Task<DeviceResponse> HandleControllerRequestAsync(DevicePayload payload)
        {
            _logger.LogInformation($"[{payload?.Uuid}] Received control request: {payload?.Type}");

            if (string.IsNullOrEmpty(payload?.Uuid))
            {
                _logger.LogError($"Device UUID is not set in payload, skipping...");
                return CreateErrorResponse($"Device UUID is not set in payload.");
            }

            //var device = await _context.Devices.FindAsync(payload.Uuid);
            var device = await GetEntity<string, Device>(_context, payload.Uuid);
            /*
            if (device == null && payload.Type != "init")
            {
                _logger.LogError($"Failed to retrieve device '{payload.Uuid}', skipping...");
                return null;
            }
            */

            switch (payload!.Type!.ToLower())
            {
                case "init":
                    return await HandleInitializeRequestAsync(payload.Uuid, device);
                case "heartbeat":
                    return await HandleHeartbeatRequestAsync(device);
                case "get_account":
                    return await HandleGetAccountAsync(device);
                case "get_job":
                    return await HandleGetJobRequestAsync(device, payload?.Username);
                case "account_banned" or
                     "account_warning" or
                     "account_invalid_credentials" or
                     "account_suspended":
                    return await HandleAccountStatusRequestAsync(device?.Uuid, device?.AccountUsername, payload?.Type);
                case "tutorial_done":
                    return await HandleTutorialStatusAsync(device?.AccountUsername);
                case "logged_out":
                    return await HandleLogoutAsync(device);
                case "job_failed":
                    _logger.LogWarning($"[{device?.Uuid}] Job failed");
                    return CreateErrorResponse("Job failed");
                default:
                    _logger.LogWarning($"[{device?.Uuid}] Unhandled request type '{payload.Type}'");
                    return CreateErrorResponse($"Unhandled request type '{payload.Type}'");
            }
        }

        #region Request Handlers

        private async Task<DeviceResponse> HandleInitializeRequestAsync(string uuid, Device? device = null)
        {
            var gitsha = GetGitHash();
            if (device is null)
            {
                // Register new device
                _logger.LogInformation($"[{uuid}] Registering new device...");
                device = new Device
                {
                    Uuid = uuid,
                };
                await _context.AddAsync(device);
                await _context.SaveChangesAsync();

                _memCache.Set(uuid, device);

                return new DeviceResponse
                {
                    Status = "ok",
                    Data = new DeviceAssignmentResponse
                    {
                        Assigned = false,
                        Version = Strings.AssemblyVersion,
                        Commit = gitsha,
                        Provider = Strings.AssemblyName,
                    },
                };
            }

            // Device is already registered
            var assignedInstance = !string.IsNullOrEmpty(device.InstanceName);
            if (!assignedInstance)
            {
                _logger.LogWarning($"[{device.Uuid}] Device is not assigned to an instance or the assigned instance is still starting!");
            }

            return new DeviceResponse
            {
                Status = "ok",
                Data = new DeviceAssignmentResponse
                {
                    Assigned = assignedInstance,
                    Version = Strings.AssemblyVersion,
                    Commit = gitsha,
                    Provider = Strings.AssemblyName,
                },
            };
        }

        private async Task<DeviceResponse> HandleHeartbeatRequestAsync(Device? device)
        {
            if (device != null)
            {
                device.LastHost = Request.GetIPAddress();
                _context.Update(device);
                await _context.SaveChangesAsync();

                _memCache.Set(device.Uuid, device);
            }
            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleGetAccountAsync(Device? device)
        {
            if (device == null)
            {
                return CreateErrorResponse($"Failed to get account, provided device was null");
            }

            var minLevel = Strings.DefaultMinimumLevel;
            var maxLevel = Strings.DefaultMaximumLevel;

            // Get instance controller for device and set min/max level variables
            var jobController = _jobControllerService.GetInstanceController(device.Uuid);
            if (jobController != null)
            {
                minLevel = jobController.MinimumLevel;
                maxLevel = jobController.MaximumLevel;
            }

            Account? account = null;
            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                var devices = _context.Devices.ToList();
                var inUseAccounts = devices
                    .Where(d => !string.IsNullOrEmpty(d.AccountUsername))
                    .OrderBy(d => d.LastSeen)
                    .Select(d => d.AccountUsername?.ToLower())
                    .ToList();

                // Get new account between min/max level and not in inUseAccount list
                account = _context.GetNewAccount(minLevel, maxLevel, Strings.DefaultSpinLimit, inUseAccounts!);

                _logger.LogDebug($"[{device.Uuid}] GetNewAccount '{account?.Username}'");
                if (account == null)
                {
                    // Failed to get new account from database
                    return CreateErrorResponse($"[{device.Uuid}] Failed to get account, are you sure you have enough acounts?");
                }

                await UpdateDeviceAsync(device, account.Username);
            }
            else
            {
                //account = await _context.Accounts.FindAsync(device.AccountUsername);
                account = await GetEntity<string, Account>(_context, device.AccountUsername);
                if (account == null)
                {
                    // Failed to get account
                    return CreateErrorResponse($"[{device.Uuid}] Failed to retrieve device's assigned account from database");
                }

                _logger.LogDebug($"[{device.Uuid}] GetOldAccount '{account.Username}'");
                if (!IsAccountValid(account, minLevel, maxLevel))
                {
                    // Current account does not meet requirements
                    await UpdateDeviceAsync(device);

                    // Switch account
                    return CreateSwitchAccountTask(minLevel, maxLevel);
                }

                // Clear pending account switch flag for device if set
                if (device.IsPendingAccountSwitch)
                {
                    device.IsPendingAccountSwitch = false;
                    _context.Update(device);
                    await _context.SaveChangesAsync();

                    _memCache.Set(device.Uuid, device);
                }
            }

            _memCache.Set(account.Username, account);

            return new DeviceResponse
            {
                Status = "ok",
                Data = new DeviceAccountResponse
                {
                    Username = account.Username.Trim(),
                    Password = account.Password.Trim(),
                    Level = account.Level,
                    FirstWarningTimestamp = account.FirstWarningTimestamp,
                },
            };
        }

        private async Task<DeviceResponse> HandleGetJobRequestAsync(Device? device, string? username)
        {
            if (device == null)
            {
                return CreateErrorResponse("Unable to get job for device, device is null");
            }

            var jobController = _jobControllerService.GetInstanceController(device.Uuid);
            if (jobController == null)
            {
                return CreateErrorResponse($"[{device.Uuid}] Failed to get job instance controller");
            }

            var minLevel = jobController.MinimumLevel;
            var maxLevel = jobController.MaximumLevel;

            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                return CreateSwitchAccountTask(minLevel, maxLevel);
            }

            // Handle account assignment changes from UI
            // REVIEW: Double check that this doesn't loop if account and in-game name differ
            if (device.AccountUsername != username)
            {
                return CreateSwitchAccountTask(minLevel, maxLevel);
            }

            Account? account = null;
            if (!string.IsNullOrEmpty(username))
            {
                // Get account by username from request payload
                //account = await _context.Accounts.FindAsync(username);
                account = await GetEntity<string, Account>(_context, username);
                if (account == null)
                {
                    // Unable to find account based on payload username, look for device's assigned account username instead
                    //account = await _context.Accounts.FindAsync(device.AccountUsername);
                    account = await GetEntity<string, Account>(_context, device.AccountUsername);
                    if (account == null)
                    {
                        _logger.LogError($"[{device.Uuid}] Failed to lookup account {username} and {device.AccountUsername} in database, switching accounts...");
                        return CreateSwitchAccountTask(minLevel, maxLevel);
                    }
                }

                if (account.Level < minLevel || account.Level > maxLevel)
                {
                    _logger.LogWarning($"[{device.Uuid}] Account {username} level {account.Level} does not meet instance {jobController.Name} level requirements between {minLevel}-{maxLevel}, switching accounts...");
                    return CreateSwitchAccountTask(minLevel, maxLevel);
                }
            }

            var options = new TaskOptions(device.Uuid, device.AccountUsername, account);
            var task = await jobController.GetTaskAsync(options);
            if (task == null)
            {
                return CreateErrorResponse($"[{device.Uuid}] No tasks available yet", logWarning: true);
            }

            _logger.LogInformation($"[{device.Uuid}] Sending {task.Action} job to {task.Latitude}, {task.Longitude}");
            return new DeviceResponse
            {
                Status = "ok",
                Data = task,
            };
        }

        private async Task<DeviceResponse> HandleAccountStatusRequestAsync(string? uuid, string? username, string? status)
        {
            // Check if device is assigned an account username
            if (string.IsNullOrEmpty(username))
            {
                return CreateErrorResponse($"[{uuid}] Device is not assigned an account!");
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            //var account = await _context.Accounts.FindAsync(device.AccountUsername);
            var account = await GetEntity<string, Account>(_context, username);
            if (account == null)
            {
                return CreateErrorResponse($"Failed to retrieve account with username '{username}'");
            }

            switch (status?.ToLower())
            {
                case "account_banned":
                    if (account.FirstWarningTimestamp == null || string.IsNullOrEmpty(account.Failed))
                    {
                        account.FailedTimestamp = now;
                        account.Failed = "banned";
                    }
                    break;
                case "account_suspended":
                    //if (account.FirstWarningTimestamp == null || string.IsNullOrEmpty(account.Failed))
                    //{
                        account.FailedTimestamp = now;
                        account.Failed = "suspended";
                    //}
                    break;
                case "account_warning":
                    if (account.FirstWarningTimestamp == null)
                    {
                        account.FirstWarningTimestamp = now;
                    }
                    break;
                case "account_invalid_credentials":
                    if (account.FirstWarningTimestamp == null || string.IsNullOrEmpty(account.Failed))
                    {
                        account.FailedTimestamp = now;
                        account.Failed = "invalid_credentials";
                    }
                    break;
                case "error_26":
                    if (account.FirstWarningTimestamp == null || string.IsNullOrEmpty(account.Failed))
                    {
                        account.FailedTimestamp = now;
                        account.Failed = "error_26";
                    }
                    break;
            }
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            _memCache.Set(account.Username, account);

            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleTutorialStatusAsync(string? username)
        {
            //var account = await _context.Accounts.FindAsync(username);
            var account = await GetEntity<string, Account>(_context, username);
            if (string.IsNullOrEmpty(username) || account == null)
            {
                return CreateErrorResponse("Failed to get account.");
            }
            if (account.Level == 0)
            {
                account.Level++;
            }
            account.Tutorial = 1;

            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            _memCache?.Set(username, account);

            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleLogoutAsync(Device? device)
        {
            if (device == null)
            {
                return CreateErrorResponse("Device does not exist");
            }

            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                var jobController = _jobControllerService.GetInstanceController(device.Uuid);
                if (jobController == null)
                {
                    return CreateErrorResponse($"[{device.Uuid}] Failed to get job instance controller to handle logout request");
                }

                var minLevel = jobController.MinimumLevel;
                var maxLevel = jobController.MaximumLevel;

                // Return switch account task
                return CreateSwitchAccountTask(minLevel, maxLevel);
            }

            // Only clear the account username if the device is pending an account switch made from the UI
            if (!device.IsPendingAccountSwitch)
            {
                await UpdateDeviceAsync(device);
            }

            _memCache.Set(device.Uuid, device);

            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        #endregion

        #region Response Handlers

        private static DeviceResponse CreateSwitchAccountTask(ushort minLevel, ushort maxLevel)
        {
            return new DeviceResponse
            {
                Status = "ok",
                Data = new SwitchAccountTask
                {
                    MinimumLevel = minLevel,
                    MaximumLevel = maxLevel,
                },
            };
        }

        private DeviceResponse CreateErrorResponse(string error, dynamic? data = null, bool logWarning = false)
        {
            if (logWarning)
            {
                _logger.LogWarning(error);
            }
            else
            {
                _logger.LogError(error);
            }
            return new DeviceResponse
            {
                Status = "error",
                Error = error,
                Data = data,
            };
        }

        #endregion

        private async Task<TEntity?> GetEntity<TKey, TEntity>(ControllerDbContext context, TKey? key)
        {
            if (key == null)
            {
                return default;
            }

            var entity = _memCache.Get<TKey, TEntity>(key);
            if (entity == null)
            {
                entity = (TEntity?)await context.FindAsync(typeof(TEntity), key);
            }
            return entity;
        }

        /// <summary>
        /// Gets the git hash value from the assembly or '--' if it cannot be found.
        /// </summary>
        /// <credits>https://stackoverflow.com/a/45248069</credits>
        private static string GetGitHash(string defaultValue = "--")
        {
            // TODO: Move to reusable project lib
            var assembly = Assembly.GetExecutingAssembly();
            var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var split = attr?.InformationalVersion?.Split('+');
            if (split?.Length == 2)
            {
                return split.Last();
            }
            return defaultValue;
        }

        private async Task UpdateDeviceAsync(Device device, string? username = null)
        {
            device.AccountUsername = username;
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();

            // Update device in cache
            _memCache.Set(device.Uuid, device);
        }

        private static bool IsAccountValid(Account? account, ushort minLevel, ushort maxLevel)
        {
            if (account == null)
                return false;

            var isValid =
                account.Level >= minLevel && account.Level <= maxLevel &&
                (account.FirstWarningTimestamp ?? 0) == 0 &&
                string.IsNullOrEmpty(account.Failed) &&
                (account.FailedTimestamp ?? 0) == 0;
            return isValid;
        }
    }
}