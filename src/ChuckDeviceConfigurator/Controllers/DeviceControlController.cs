namespace ChuckDeviceConfigurator.Controllers;

using System.Reflection;

using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.Services.Jobs;
using ChuckDeviceConfigurator.Utilities;
using ChuckDeviceController.Caching.Memory;
using ChuckDeviceController.Common;
using ChuckDeviceController.Common.Tasks;
using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Http;
using ChuckDeviceController.JobControllers.Tasks;
using ChuckDeviceController.Net.Models.Requests;
using ChuckDeviceController.Net.Models.Responses;
using Elfie.Serialization;

[ApiController]
public class DeviceControlController : ControllerBase
{
    private const string ContentTypeJson = "application/json";
    private const ushort AccountLastUsedS = 1800; // 30 minutes

    #region Variables

    private static readonly Random _rand = new();
    private readonly ILogger<DeviceControlController> _logger;
    private readonly IUnitOfWork _uow;
    private readonly IJobControllerService _jobControllerService;
    private readonly IMemoryCacheService _memCache;
    private readonly ILoginLimiter _limiter;

    #endregion

    #region Constructor

    public DeviceControlController(
        ILogger<DeviceControlController> logger,
        IUnitOfWork uow,
        IJobControllerService jobControllerService,
        IMemoryCacheService memCache,
        ILoginLimiter limiter)
    {
        _logger = logger;
        _uow = uow;
        _jobControllerService = jobControllerService;
        _memCache = memCache;
        _limiter = limiter;
    }

    #endregion

    #region Routes

#if DEBUG
    [
        Route("/controler"),
        Route("/controller"),
        HttpGet(),
    ]
    public string GetAsync() => ":)";
#endif

    [
        Route("/controler"),
        Route("/controller"),
        HttpPost(),
        Produces(ContentTypeJson),
    ]
    public async Task<DeviceResponse> PostAsync(DevicePayload payload)//object body)
    {
        //{"type":"init","uuid":"iPhone","username":"0011223344","timestamp":1669798714537}
        //{"type":"get_account","uuid":"iPhone","username":"0011223344","timestamp":1669799065184,"min_level":30,"max_level":36}
        //{"type":"get_job","uuid":"iPhone","username":"0011223344","timestamp":1669798714537}
        //{"type":"get_job","uuid":"atv08","trainerlvl":5,"trainerexp":12210,"username":"0W3EiL8Ihgq"}

        Response.Headers["Accept"] = ContentTypeJson;
        Response.Headers["Content-Type"] = ContentTypeJson;

        //_logger.LogInformation($"CDC: {body}");
        //var payload = body.ToString().FromJson<DevicePayload>();

        var response = await HandleControllerRequestAsync(payload);
        return response;
    }

    #endregion

    #region Route Handlers

    private async Task<DeviceResponse> HandleControllerRequestAsync(DevicePayload payload)
    {
        _logger.LogInformation($"[{payload?.Uuid}] Received control request: {payload?.Type}");

        if (string.IsNullOrEmpty(payload?.Uuid))
        {
            _logger.LogError($"Device UUID is not set in payload, skipping...");
            return CreateErrorResponse($"Device UUID is not set in payload.");
        }

        //var device = await GetEntityAsync<string, Device>(_context, payload.Uuid);
        var device = await _uow.Devices.FindByIdAsync(payload.Uuid);

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
                return await HandleTutorialStatusRequestAsync(device?.AccountUsername);
            case "logged_out":
                return await HandleLogoutRequestAsync(device);
            case "job_failed":
                _logger.LogWarning($"[{device?.Uuid}] Job failed");
                return CreateErrorResponse("Job failed");
            default:
                _logger.LogWarning($"[{device?.Uuid}] Unhandled request type '{payload.Type}'");
                return CreateErrorResponse($"Unhandled request type '{payload.Type}'");
        }
    }

    #endregion

    #region Request Handlers

    private async Task<DeviceResponse> HandleInitializeRequestAsync(string uuid, Device? device = null)
    {
        var gitsha = GitHub.GetGitHash(Assembly.GetExecutingAssembly());
        if (device is null)
        {
            // Register new device
            _logger.LogInformation($"[{uuid}] Registering new device...");
            var ipAddr = Request.GetIPAddress();
            device = new Device
            {
                Uuid = uuid,
                LastHost = ipAddr,
            };

            await _uow.Devices.AddAsync(device);
            await _uow.CommitAsync();
            _memCache.Set(uuid, device);
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
        if (device is null)
        {
            return CreateErrorResponse("Failed to handle heartbeat, provided device was null");
        }

        var ipAddr = Request.GetIPAddress();
        if (device.LastHost != ipAddr)
        {
            device.LastHost = ipAddr;
            await _uow.Devices.UpdateAsync(device);
            await _uow.CommitAsync();
        }
        return new DeviceResponse
        {
            Status = "ok",
        };
    }

    private async Task<DeviceResponse> HandleGetAccountAsync(Device? device)
    {
        if (device is null)
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

        Account? account;
        if (string.IsNullOrEmpty(device.AccountUsername))
        {
            // Get new account between min/max level and not already in use
            account = await GetNewAccountAsync(minLevel, maxLevel);
            _logger.LogDebug($"[{device.Uuid}] GetNewAccount '{account?.Username}' (Status: {account?.Status}, Level: {account?.Level})");
            if (account == null)
            {
                // Failed to get new account from database
                return CreateErrorResponse($"[{device.Uuid}] Failed to get account, are you sure you have enough acounts?");
            }

            if (!account.IsValid(minLevel, maxLevel, ignoreWarning: false, groupName: null))
            {
                device.AccountUsername = null;
            }
            else
            {
                device.AccountUsername = account.Username;
            }
            await _uow.Devices.UpdateAsync(device);
            await _uow.CommitAsync();
        }
        else
        {
            account = await _uow.Accounts.FindByIdAsync(device.AccountUsername);
            if (account == null)
            {
                // Failed to get account
                return CreateErrorResponse($"[{device.Uuid}] Failed to retrieve account assigned to device from database '{device.AccountUsername}'.");
            }

            _logger.LogDebug($"[{device.Uuid}] GetOldAccount '{account.Username}' (Status: {account.Status}, Level: {account.Level})");
            if (!account.IsValid(minLevel, maxLevel, ignoreWarning: false, groupName: null))
            {
                _logger.LogWarning($"[{device.Uuid}] Assigned account '{account.Username}' is no longer valid with status '{account.Status}', switching accounts...");

                // Current account does not meet requirements
                device.AccountUsername = null;
                await _uow.Devices.UpdateAsync(device);
                await _uow.CommitAsync();

                // Remove account from cache
                _memCache.Unset<string, Account>(account.Username);

                // Switch account
                return CreateSwitchAccountTask(minLevel, maxLevel);
            }

            // Clear pending account switch flag for device if set
            if (device.IsPendingAccountSwitch)
            {
                _logger.LogDebug($"[{device.Uuid}] Pending manual account switch, reverting flag to prevent loop.");

                device.IsPendingAccountSwitch = false;
                await _uow.Devices.UpdateAsync(device);
                await _uow.CommitAsync();
                _memCache.Set(device.Uuid, device);
            }
        }

        // Login limiter
        if (_limiter.Options.Enabled && device.AccountUsername != account.Username)
        {
            var host = Request.GetIPAddress();
            var (isRateLimited, retryAfter) = await _limiter.IsLimitedAsync(host!);
            if (isRateLimited)
            {
                Response.StatusCode = 429;
                Response.Headers.Add("Retry-After", retryAfter.ToString());
                await Response.WriteAsync("Login limit exceeded");
                return CreateErrorResponse("Login limit exceeded");
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
        if (device.AccountUsername != username)
        {
            return CreateSwitchAccountTask(minLevel, maxLevel);
        }

        IAccount? account = null;
        if (!string.IsNullOrEmpty(username))
        {
            // Get account by username from request payload
            account = await _uow.Accounts.FindByIdAsync(username);
            if (account == null)
            {
                // Unable to find account based on payload username, look for device's assigned account username instead
                account = await _uow.Accounts.FindByIdAsync(device.AccountUsername);
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

        if (account != null)
        {
            _memCache.Set(account.Username, account);
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
            return CreateErrorResponse($"[{uuid}] Device is not assigned to an account!");
        }

        var now = DateTime.UtcNow.ToTotalSeconds();
        var account = await _uow.Accounts.FindByIdAsync(username);
        if (account == null)
        {
            return CreateErrorResponse($"Failed to retrieve account with username '{username}'");
        }

        var oldStatus = account.Status;
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
                if (account.FirstWarningTimestamp == null || string.IsNullOrEmpty(account.Failed))
                {
                    account.FailedTimestamp = now;
                    account.Failed = "suspended";
                }
                break;
            case "account_warning":
                account.FirstWarningTimestamp ??= now;
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

        if (oldStatus != account.Status)
        {
            // TODO: Send webhook for account status change
            _logger.LogInformation($"Status changed for account '{account.Username}' from '{oldStatus}' to '{account.Status}'.");
        }

        await _uow.Accounts.UpdateAsync(account);
        await _uow.CommitAsync();
        _memCache.Set(account.Username, account);

        return new DeviceResponse
        {
            Status = "ok",
        };
    }

    private async Task<DeviceResponse> HandleTutorialStatusRequestAsync(string? username)
    {
        var account = await _uow.Accounts.FindByIdAsync(username);
        if (string.IsNullOrEmpty(username) || account == null)
        {
            return CreateErrorResponse("Failed to get account.");
        }
        if (account.Level == 0)
        {
            account.Level++;
        }
        account.Tutorial = 1;

        await _uow.Accounts.UpdateAsync(account);
        await _uow.CommitAsync();
        _memCache.Set(account.Username, account);

        return new DeviceResponse
        {
            Status = "ok",
        };
    }

    private async Task<DeviceResponse> HandleLogoutRequestAsync(Device? device)
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

        // Clear the account username if the device is not pending an account switch made from the UI
        // otherwise this will lead to a continuous loop
        if (!device.IsPendingAccountSwitch)
        {
            device.AccountUsername = null;
            await _uow.Devices.UpdateAsync(device);
            await _uow.CommitAsync();

            _memCache.Set(device.Uuid, device);
        }

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

    #region Helper Methods

    private async Task<Account?> GetNewAccountAsync(ushort minLevel, ushort maxLevel)
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var devices = await _uow.Devices.FindAllAsync();
        var inUseAccounts = devices
            .Where(d => !string.IsNullOrEmpty(d.AccountUsername))
            .OrderBy(d => d.LastSeen)
            .Select(d => d.AccountUsername?.ToLower())
            .ToList();

        var accounts = await _uow.Accounts.FindAsync(x =>
            x.Level >= minLevel && x.Level <= maxLevel &&
            x.IsAccountClean &&
            (x.LastUsedTimestamp == null || (x.LastUsedTimestamp != null && x.LastUsedTimestamp > 0 && now - AccountLastUsedS > x.LastUsedTimestamp)) &&
            !inUseAccounts.Contains(x.Username.ToLower())
        );

        var matchCount = accounts.Count();
        if (matchCount == 0)
        {
            return accounts.FirstOrDefault();
        }
        return accounts.ElementAt(_rand.Next(0, matchCount));
    }

    //private async Task<TEntity?> GetEntityAsync<TKey, TEntity>(ControllerDbContext context, TKey? key, bool skipCache = true)
    //    where TEntity : class
    //{
    //    if (key == null)
    //    {
    //        return default;
    //    }

    //    TEntity? entity = null;
    //    if (!skipCache)
    //    {
    //        entity = _memCache.Get<TKey, TEntity>(key);
    //    }

    //    entity ??= await context.Set<TEntity>().FindAsync(key);
    //    return entity;
    //}

    //private async Task SetEntityAsync<TKey, TEntity>(TKey key, TEntity entity, bool skipCache = true)
    //    where TEntity : class
    //{
    //    //_context.Set<TEntity>().Update(entity);
    //    //await _context.SaveChangesAsync();
    //    await _uow.CommitAsync();

    //    if (!skipCache)
    //    {
    //        // Update entity in cache
    //        _memCache.Set(key, entity);
    //    }
    //}

    #endregion
}