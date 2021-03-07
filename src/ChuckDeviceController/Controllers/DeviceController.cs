namespace ChuckDeviceController.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Data.Repositories;
    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.JobControllers.Tasks;
    using ChuckDeviceController.JobControllers;
    using ChuckDeviceController.Net.Models.Requests;
    using ChuckDeviceController.Net.Models.Responses;

    [ApiController]
    public class DeviceController : ControllerBase
    {
        #region Variables

        private readonly DeviceRepository _deviceRepository;
        private readonly AccountRepository _accountRepository;
        //private readonly DeviceControllerContext _context;
        private readonly IDbContextFactory<DeviceControllerContext> _dbFactory;
        private readonly ILogger<DeviceController> _logger;

        #endregion

        #region Constructor

        public DeviceController(IDbContextFactory<DeviceControllerContext> dbFactory, ILogger<DeviceController> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;

            _deviceRepository = new DeviceRepository(_dbFactory.CreateDbContext());
            _accountRepository = new AccountRepository(_dbFactory.CreateDbContext());
        }

        #endregion

        #region Routes

        [HttpGet("/controler")]
        [HttpGet("/controller")]
        public string Get() => ":D";

        [
            HttpPost("/controler"),
            HttpPost("/controller"),
            Produces("application/json"),
        ]
        public async Task<DeviceResponse> PostAsync(DevicePayload payload)
        {
            var response = await HandleControllerRequest(payload).ConfigureAwait(false);
            if (response == null)
            {
                _logger.LogError($"[Device] [{payload.Uuid}] null data response!");
            }
            Response.Headers["Accept"] = "application/json";
            Response.Headers["Content-Type"] = "application/json";
            return response;
        }

        #endregion

        #region Handlers

        private async Task<DeviceResponse> HandleControllerRequest(DevicePayload payload)
        {
            _logger.LogInformation($"[Device] [{payload.Uuid}] Received control request: {payload.Type}");

            var device = await _deviceRepository.GetByIdAsync(payload.Uuid).ConfigureAwait(false);
            switch (payload.Type.ToLower())
            {
                case "init":
                    return await HandleInitialize(device, payload.Uuid).ConfigureAwait(false);
                case "heartbeat":
                    return await HandleHeartbeat(payload.Uuid).ConfigureAwait(false);
                case "get_job":
                    return await HandleGetJob(device, payload.Username).ConfigureAwait(false);
                case "get_account":
                    return await HandleGetAccount(device).ConfigureAwait(false);
                case "account_banned":
                case "account_warning":
                case "account_invalid_credentials":
                    return await HandleAccountStatus(device, payload.Type).ConfigureAwait(false);
                case "tutorial_done":
                    return await HandleTutorialStatus(device).ConfigureAwait(false);
                case "logged_out":
                    return await HandleLogout(device).ConfigureAwait(false);
                case "job_failed":
                    _logger.LogWarning($"[Device] [{device.Uuid}] Job failed");
                    return new DeviceResponse
                    {
                        Status = "error",
                        Error = "Job failed",
                    };
                default:
                    _logger.LogWarning($"[Device] [{device?.Uuid}] Unhandled request type: {payload.Type}");
                    return new DeviceResponse
                    {
                        Status = "error",
                        Error = $"Unhandled request type: {payload.Type}",
                    };
            }
        }

        #endregion

        #region Request Handlers

        private async Task<DeviceResponse> HandleInitialize(Device device, string uuid)
        {
            ulong? firstWarningTimestamp = null;
            if (!string.IsNullOrEmpty(device?.AccountUsername))
            {
                var account = await _accountRepository.GetByIdAsync(device.AccountUsername).ConfigureAwait(false);
                firstWarningTimestamp = account?.FirstWarningTimestamp;
            }
            if (device is Device)
            {
                // Device is already registered
                _logger.LogDebug($"[Device] [{device.Uuid}] Device already registered");
                var assignedInstance = !string.IsNullOrEmpty(device.InstanceName);
                if (!assignedInstance)
                {
                    _logger.LogWarning($"[Device] [{device.Uuid}] Device is not assigned to an instance!");
                }
                return new DeviceResponse
                {
                    Status = "ok",
                    Data = new DeviceAssignmentResponse
                    {
                        Assigned = assignedInstance,
                        FirstWarningTimestamp = firstWarningTimestamp,
                    }
                };
            }

            // Register new device
            _logger.LogDebug($"[Device] [{uuid}] Registering device");
            await _deviceRepository.AddAsync(new Device
            {
                Uuid = uuid,
                AccountUsername = null,
                InstanceName = null,
                LastHost = null,
                LastLatitude = 0,
                LastLongitude = 0,
            }).ConfigureAwait(false);
            return new DeviceResponse
            {
                Status = "ok",
                Data = new DeviceAssignmentResponse
                {
                    Assigned = false,
                    FirstWarningTimestamp = firstWarningTimestamp,
                }
            };
        }

        private async Task<DeviceResponse> HandleHeartbeat(string uuid)
        {
            var device = await _deviceRepository.GetByIdAsync(uuid).ConfigureAwait(false);
            if (device != null)
            {
                var cfHeader = Request.Headers["cf-connecting-ip"].ToString();
                var forwardedfor = Request.Headers["x-forwarded-for"].ToString()?.Split(",").FirstOrDefault();
                var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
                var localIp = Request.HttpContext.Connection.LocalIpAddress?.ToString();
                device.LastHost = !string.IsNullOrEmpty(cfHeader)
                    ? cfHeader
                    : !string.IsNullOrEmpty(forwardedfor)
                        ? forwardedfor
                        : !string.IsNullOrEmpty(remoteIp)
                            ? remoteIp
                            : !string.IsNullOrEmpty(localIp)
                                ? localIp
                                : string.Empty;
                await _deviceRepository.UpdateAsync(device).ConfigureAwait(false);
            }
            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleGetAccount(Device device)
        {
            ushort minLevel = 0;
            ushort maxLevel = 29;

            if (device != null)
            {
                var instanceController = InstanceController.Instance.GetInstanceController(device.Uuid);
                if (instanceController != null)
                {
                    minLevel = instanceController.MinimumLevel;
                    maxLevel = instanceController.MaximumLevel;
                }
            }

            // TODO: Check if account in database (username from payload)
            Account account = null;
            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                // Device not assigned an account, fetch a new one
                var devices = await _deviceRepository.GetAllAsync().ConfigureAwait(false);
                var inuseAccounts = devices.Where(x => !string.IsNullOrEmpty(x.AccountUsername))
                                           .Select(y => y.AccountUsername.ToLower())
                                           .ToList();
                account = await _accountRepository.GetNewAccountAsync(minLevel, maxLevel, inuseAccounts).ConfigureAwait(false);
                _logger.LogDebug($"[Device] [{device.Uuid}] GetNewAccount ${account?.Username}");
                if (account == null)
                {
                    // Failed to get new account from database
                    _logger.LogError($"[Device] [{device.Uuid}] Failed to get account. Make sure you have accounts in your 'account' table.");
                    return new DeviceResponse
                    {
                        Status = "error",
                        Error = "Failed to get account, are you sure you have accounts?",
                    };
                }
            }
            else
            {
                account = await _accountRepository.GetByIdAsync(device.AccountUsername).ConfigureAwait(false);
                // Assigned account exists in database
                _logger.LogDebug($"[Device] [{device.Uuid}] GetOldAccount {account?.Username}");
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
                            FirstWarningTimestamp = account.FirstWarningTimestamp,
                            Level = account.Level,
                        }
                    };
                }
                else
                {
                    // Switch account
                    return CreateSwitchAccountTask(minLevel, maxLevel);
                }
            }
            device.AccountUsername = account.Username;
            //device.DeviceLevel = account.Level;
            await _deviceRepository.AddOrUpdateAsync(device).ConfigureAwait(false);
            return new DeviceResponse
            {
                Status = "ok",
                Data = new DeviceAccountResponse
                {
                    Username = account.Username.Trim(),
                    Password = account.Password.Trim(),
                    FirstWarningTimestamp = account.FirstWarningTimestamp,
                    Level = account.Level,
                }
            };
        }

        private async Task<DeviceResponse> HandleGetJob(Device device, string username)
        {
            var instanceController = InstanceController.Instance.GetInstanceController(device.Uuid);
            if (instanceController == null)
            {
                _logger.LogError($"[Device] [{device.Uuid}] Failed to get instance controller.");
                return new DeviceResponse
                {
                    Status = "error",
                    Error = "Failed to get instance controller",
                };
            }
            var minLevel = instanceController.MinimumLevel;
            var maxLevel = instanceController.MaximumLevel;
            // If account nulled out in database, fetch new one
            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                return CreateSwitchAccountTask(minLevel, maxLevel);
            }
            if (!string.IsNullOrEmpty(username))
            {
                var account = await _accountRepository.GetByIdAsync(username).ConfigureAwait(false);
                if (account == null)
                {
                    account = await _accountRepository.GetByIdAsync(device.AccountUsername).ConfigureAwait(false);
                    if (account == null)
                    {
                        _logger.LogWarning($"[Device] [{device.Uuid}] Failed to lookup account {username} in database, switching accounts...");
                        // Switch account
                        return CreateSwitchAccountTask(minLevel, maxLevel);
                    }
                }
                if (account.Level < minLevel || account.Level > maxLevel)
                {
                    _logger.LogWarning($"[Device] [{device.Uuid}] Account {username} level {account.Level} does not meet instance {instanceController.Name} level requirements between {minLevel}-{maxLevel}, switching accounts...");
                    // Switch account
                    return CreateSwitchAccountTask(minLevel, maxLevel);
                }
            }

            var task = await instanceController.GetTask(device.Uuid, device.AccountUsername, false).ConfigureAwait(false);
            if (task == null)
            {
                _logger.LogWarning($"[Device] [{device.Uuid}] No tasks avaialable yet");
                return new DeviceResponse
                {
                    Status = "error",
                    Error = "No tasks available yet",
                };
            }
            _logger.LogInformation($"[Device] [{device.Uuid}] Sending {task.Action} job to {task.Latitude}, {task.Longitude}");
            return new DeviceResponse
            {
                Status = "ok",
                Data = task,
            };
        }

        private async Task<DeviceResponse> HandleAccountStatus(Device device, string type)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var account = await _accountRepository.GetByIdAsync(device.AccountUsername).ConfigureAwait(false);
            if (account == null)
            {
                return new DeviceResponse
                {
                    Status = "error",
                    Error = $"Failed to lookup account by username {device.AccountUsername}",
                };
            }

            switch (type.ToLower())
            {
                case "account_banned":
                    if (account.FirstWarningTimestamp == null || string.IsNullOrEmpty(account.Failed))
                    {
                        account.FailedTimestamp = now;
                        account.Failed = "banned";
                    }
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
            }
            //await _accountRepository.UpdateAsync(account);
            await _accountRepository.AddOrUpdateAsync(account).ConfigureAwait(false);
            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleTutorialStatus(Device device)
        {
            var username = device.AccountUsername;
            var account = await _accountRepository.GetByIdAsync(username).ConfigureAwait(false);
            if (device == null || string.IsNullOrEmpty(username) || account == null)
            {
                return new DeviceResponse
                {
                    Status = "error",
                    Error = "Failed to get account.",
                };
            }
            if (account.Level == 0)
            {
                account.Level++;
            }
            account.Tutorial = 1;
            await _accountRepository.UpdateAsync(account).ConfigureAwait(false);
            return new DeviceResponse
            {
                Status = "ok",
            };
        }

        private async Task<DeviceResponse> HandleLogout(Device device)
        {
            if (device == null)
            {
                return new DeviceResponse
                {
                    Status = "error",
                    Error = "Device does not exist"
                };
            }
            if (string.IsNullOrEmpty(device.AccountUsername))
            {
                return new DeviceResponse
                {
                    Status = "error",
                    Error = "Account does not exist",
                };
            }
            device.AccountUsername = null;
            await _deviceRepository.AddOrUpdateAsync(device).ConfigureAwait(false);
            return new DeviceResponse
            {
                Status = "ok",
            };
        }

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

        #endregion
    }
}