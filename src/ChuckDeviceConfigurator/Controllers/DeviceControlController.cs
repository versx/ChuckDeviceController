namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Extensions;
    using ChuckDeviceConfigurator.Net.Models.Requests;
    using ChuckDeviceConfigurator.Net.Models.Responses;
    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceConfigurator.Services.Tasks;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions;

    [ApiController]
    public class DeviceControlController : ControllerBase
    {
        private readonly ILogger<DeviceControlController> _logger;
        private readonly DeviceControllerContext _context;
        private readonly IJobControllerService _jobControllerService;

        public DeviceControlController(
            ILogger<DeviceControlController> logger,
            DeviceControllerContext context,
            IJobControllerService jobControllerService)
        {
            _logger = logger;
            _context = context;
            _jobControllerService = jobControllerService;
        }

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
            _logger.LogInformation($"[{payload.Uuid}] Received control request: {payload.Type}");

            if (string.IsNullOrEmpty(payload?.Uuid))
            {
                _logger.LogError($"Device UUID is not set in payload, skipping...");
                return CreateErrorResponse($"Device UUID is not set in payload.");
            }

            var device = await _context.Devices.FindAsync(payload.Uuid);
            /*
            if (device == null && payload.Type != "init")
            {
                _logger.LogError($"Failed to retrieve device '{payload.Uuid}', skipping...");
                return null;
            }
            */

            switch (payload.Type.ToLower())
            {
                case "init":
                    return await HandleInitializeRequestAsync(payload.Uuid, device);
                case "heartbeat":
                    return await HandleHeartbeatRequestAsync(device);
                case "get_account":
                    return await HandleGetAccountAsync(device);
                case "get_job":
                    return await HandleGetJobRequestAsync(device, payload.Username);
                case "account_banned" or
                     "account_warning" or
                     "account_invalid_credentials" or
                     "account_suspended":
                    return await HandleAccountStatusRequestAsync(device, payload.Type);
                case "tutorial_done":
                    return await HandleTutorialStatusAsync(device);
                case "logged_out":
                    return await HandleLogoutAsync(device);
                case "job_failed":
                    _logger.LogWarning($"[{device.Uuid}] Job failed");
                    return CreateErrorResponse("Job failed");
                default:
                    _logger.LogWarning($"[{device.Uuid}] Unhandled request type '{payload.Type}'");
                    return CreateErrorResponse($"Unhandled request type '{payload.Type}'");
            }
        }

        #region Request Handlers

        private async Task<DeviceResponse> HandleInitializeRequestAsync(string uuid, Device? device = null)
        {
            if (device is not null)
            {
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
                        Commit = "", // TODO: Get git commit
                        Provider = Strings.AssemblyName,
                    },
                };
            }

            // Register new device
            _logger.LogDebug($"[{uuid}] Registering new device...");
            await _context.AddAsync(new Device
            {
                Uuid = uuid,
            });
            await _context.SaveChangesAsync();

            return new DeviceResponse
            {
                Status = "ok",
                Data = new DeviceAssignmentResponse
                {
                    Assigned = false,
                    Version = Strings.AssemblyVersion,
                    Commit = "", // TODO: Get git commit
                    Provider = Strings.AssemblyName,
                },
            };
        }

        private async Task<DeviceResponse> HandleHeartbeatRequestAsync(Device device)
        {
            if (device != null)
            {
                device.LastHost = Request.GetIPAddress();
                _context.Update(device);
                await _context.SaveChangesAsync();
            }
            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleGetAccountAsync(Device device)
        {
            var minLevel = Strings.DefaultMinimumLevel;
            var maxLevel = Strings.DefaultMaximumLevel;

            if (device is not null)
            {
                // Get instance controller for device and set min/max level variables
                var jobController = _jobControllerService.GetInstanceController(device.Uuid);
                if (jobController != null)
                {
                    minLevel = jobController.MinimumLevel;
                    maxLevel = jobController.MaximumLevel;
                }
            }

            Account? account = null;
            if (string.IsNullOrEmpty(device?.AccountUsername))
            {
                var devices = _context.Devices.ToList();
                var inUseAccounts = devices.Where(d => !string.IsNullOrEmpty(d.AccountUsername))
                                           .OrderBy(d => d.LastSeen)
                                           .Select(d => d.AccountUsername?.ToLower())
                                           .ToList();

                // Get new account between min/max level and not in inUseAccount list
                account = _context.GetNewAccount(minLevel, maxLevel, Strings.DefaultSpinLimit, inUseAccounts);

                _logger.LogDebug($"[{device?.Uuid}] GetNewAccount '{account?.Username}'");
                if (account == null)
                {
                    // Failed to get new account from database
                    _logger.LogError($"[{device.Uuid}] Failed to get account, make sure you have accounts in your `account` table.");
                    return CreateErrorResponse("Failed to get account, are you sure you have enough acounts?");
                }
            }
            else
            {
                account = await _context.Accounts.FindAsync(device.AccountUsername);
                if (account == null)
                {
                    // Failed to get account
                    _logger.LogError($"[{device.Uuid}] Failed to retrieve device's assigned account from database");
                    return CreateErrorResponse($"Failed to retrieve device's assigned account from database");
                }

                _logger.LogDebug($"[{device.Uuid}] GetOldAccount '{account?.Username}'");
                if (account.Level >= minLevel &&
                    account.Level <= maxLevel &&
                    (!account.FirstWarningTimestamp.HasValue || account.FirstWarningTimestamp == 0) &&
                    string.IsNullOrEmpty(account.Failed) &&
                    (!account.FailedTimestamp.HasValue || account.FailedTimestamp > 0))
                {
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
                else
                {
                    // Switch account
                    return CreateSwitchAccountTask(minLevel, maxLevel);
                }
            }

            device.AccountUsername = account.Username;
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();

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

        private async Task<DeviceResponse> HandleGetJobRequestAsync(Device device, string username)
        {
            if (device == null)
            {
                _logger.LogError($"Unable to get job for device, device is null");
                return CreateErrorResponse("Unable to get job for device, device is null");
            }

            var jobController = _jobControllerService.GetInstanceController(device.Uuid);
            if (jobController == null)
            {
                _logger.LogError($"[{device.Uuid}] Failed to get job instance controller");
                return CreateErrorResponse($"Failed to get job instance controller");
            }

            var minLevel = jobController.MinimumLevel;
            var maxLevel = jobController.MaximumLevel;

            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                return CreateSwitchAccountTask(minLevel, maxLevel);
            }

            Account? account = null;
            if (!string.IsNullOrEmpty(username))
            {
                // Get account by username from request payload
                account = await _context.Accounts.FindAsync(username);
                if (account == null)
                {
                    // Unable to find account based on payload username, look for device's assigned account username instead
                    account = await _context.Accounts.FindAsync(device.AccountUsername);
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

            var task = await jobController.GetTaskAsync(new TaskOptions
            {
                Uuid = device.Uuid,
                AccountUsername = device.AccountUsername,
                Account = account,
            });
            if (task == null)
            {
                _logger.LogWarning($"[{device.Uuid}] No tasks avaialable yet");
                return CreateErrorResponse("No tasks available yet");
            }

            _logger.LogInformation($"[{device?.Uuid}] Sending {task.Action} job to {task.Latitude}, {task.Longitude}");
            return new DeviceResponse
            {
                Status = "ok",
                Data = task,
            };
        }

        private async Task<DeviceResponse> HandleAccountStatusRequestAsync(Device device, string status)
        {
            // Check if device is assigned an account username
            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                return CreateErrorResponse($"Device '{device.Uuid}' is not assigned an account!");
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            var account = await _context.Accounts.FindAsync(device.AccountUsername);
            if (account == null)
            {
                return CreateErrorResponse($"Failed to retrieve account with username '{device.AccountUsername}'");
            }

            switch (status.ToLower())
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

            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleTutorialStatusAsync(Device device)
        {
            var username = device.AccountUsername;
            var account = await _context.Accounts.FindAsync(username);
            if (device == null || string.IsNullOrEmpty(username) || account == null)
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

            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleLogoutAsync(Device device)
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
                    _logger.LogError($"[{device.Uuid}] Failed to get job instance controller to handle logout request");
                    return CreateErrorResponse($"Failed to get job instance controller to handle logout request");
                }

                var minLevel = jobController.MinimumLevel;
                var maxLevel = jobController.MaximumLevel;

                // Return switch account task
                return CreateSwitchAccountTask(minLevel, maxLevel);
                //return CreateErrorResponse("Device is not assigned an account, unable handle logout request");
            }

            device.AccountUsername = null;
            _context.Devices.Update(device);
            await _context.SaveChangesAsync();

            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        #endregion

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

        private DeviceResponse CreateErrorResponse(string error, dynamic? data = null)
        {
            _logger.LogError(error);
            return new DeviceResponse
            {
                Status = "error",
                Error = error,
                Data = data,
            };
        }
    }

    public static class DbContextEntityExtensions
    {
        public static Account GetNewAccount(this DeviceControllerContext context, ushort minLevel, ushort maxLevel, uint maxSpins = 3500, IReadOnlyList<string> accountsInUse = null)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var account = context.Accounts.FirstOrDefault(x =>
                x.Level >= minLevel &&
                x.Level <= maxLevel &&
                string.IsNullOrEmpty(x.Failed) &&
                x.Spins < maxSpins &&
                x.LastEncounterTime == null &&
                (x.LastUsedTimestamp == null || (x.LastUsedTimestamp > 0 && now - x.LastUsedTimestamp >= Strings.ThirtyMinutesS)) &&
                x.FirstWarningTimestamp == null &&
                (x.Warn == null || !(x.Warn ?? false)) &&
                (x.WarnExpireTimestamp == null || x.WarnExpireTimestamp == 0) &&
                x.Banned == null &&
                !accountsInUse.Contains(x.Username.ToLower())
            );
            return account;
        }
    }
}