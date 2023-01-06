namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.Services.Jobs;
using ChuckDeviceController.Caching.Memory;
using ChuckDeviceController.Common.Jobs;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Extensions;
using ChuckDeviceController.Geometry.Models;

[ApiController]
public class ApiController : ControllerBase
{
    #region Variables

    private readonly ILogger<ApiController> _logger;
    private readonly IJobControllerService _jobControllerService;
    private readonly IMemoryCacheService _memCache;

    #endregion

    #region Constructor

    public ApiController(
        ILogger<ApiController> logger,
        IJobControllerService jobControllerService,
        IMemoryCacheService memCache)
    {
        _logger = logger;
        _jobControllerService = jobControllerService;
        _memCache = memCache;
    }

    #endregion

    [HttpGet("api/test")]
    public IActionResult Index()
    {
        return Content(":)");
    }

    [HttpPost("api/scannext/add")]
    public JsonResult AddScanNext([FromQuery] string instanceName, [FromBody] List<Coordinate> coords)
    {
        _logger.LogInformation($"Received ScanNext API request for job controller instance '{instanceName}' with {coords.Count:N0} coordinates");

        var jobController = _jobControllerService.GetInstanceControllerByName(instanceName);
        if (jobController == null)
        {
            _logger.LogError($"Failed to get a ScanNext job controller with name {instanceName}");
            return new JsonResult(new BadRequestResult());
        }

        if ((coords?.Count ?? 0) == 0)
        {
            _logger.LogError($"[{jobController.Name}] No coordinates provided for ScanNext API request, ignoring");
            return new JsonResult(new BadRequestResult());
        }

        if (jobController is not IScanNextInstanceController scanNextController)
        {
            _logger.LogWarning($"[{jobController.Name}] Job controller instance does not support ScanNext API");
            return new JsonResult(new BadRequestResult());
        }

        var devices = _jobControllerService.GetDeviceUuidsInInstance(jobController.Name);
        if (!(devices?.Any() ?? false))
        {
            _logger.LogWarning($"[{jobController.Name}] Job controller instance does not have any devices assigned to it, unable to complete ScanNext API request");
            return new JsonResult(new BadRequestResult());
        }

        foreach (var coord in coords!)
        {
            _logger.LogInformation($"[{jobController.Name}] Queuing coordinate '{coord}' to ScanNext job controller instance");
            scanNextController.ScanNextCoordinates.Enqueue(coord);
        }

        _logger.LogInformation($"[{jobController.Name}] ScanNext API list currently has {scanNextController.ScanNextCoordinates.Count:N0} coordinates queued");

        var obj = new
        {
            action = "next_scan",
            size = scanNextController.ScanNextCoordinates.Count,
            timestamp = DateTime.UtcNow.ToTotalSeconds(),
        };
        return new JsonResult(obj);
    }

    [HttpPost("api/instance/reload/{instanceName?}")]
    public async Task<JsonResult> ReloadInstance(string? instanceName = null)
    {
        if (string.IsNullOrEmpty(instanceName))
        {
            _jobControllerService.ReloadAllInstances();
            _logger.LogDebug($"Reloading all instances");
        }
        else
        {
            await _jobControllerService.ReloadInstanceAsync(instanceName).ConfigureAwait(false);
            _logger.LogDebug($"Reloading instance '{instanceName}'");
        }
        var obj = new
        {
            status = "ok",
        };
        return new JsonResult(obj);
    }

    [HttpPost("api/instance/quests/clear")]
    public async Task<JsonResult> ClearQuests()
    {
        await Task.CompletedTask;
        return new JsonResult(new
        {
            status = "ok",
        });
    }

    [HttpPost("api/gym/set_details/{id}")]
    public async Task<JsonResult> SetGymDetails(string id, FortDetailsPayload payload)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new JsonResult(new
            {
                status = "error",
            });
        }

        await Task.CompletedTask;
        return new JsonResult(new
        {
            status = "ok",
        });
    }

    [HttpPost("api/pokestop/set_details/{id}")]
    public async Task<JsonResult> SetPokestopDetails(string id, FortDetailsPayload payload)
    {
        if (string.IsNullOrEmpty(id))
        {
            return new JsonResult(new
            {
                status = "error",
            });
        }

        await Task.CompletedTask;
        return new JsonResult(new
        {
            status = "ok",
        });
    }

    [HttpPost("api/cache/clear")]
    public async Task<JsonResult> ClearCache()
    {
        if (!_memCache.Clear<Account>())
        {
            return new JsonResult(new
            {
                status = "error",
                message = "Failed to clear account memory cache.",
            });
        }

        if (!_memCache.Clear<Device>())
        {
            return new JsonResult(new
            {
                status = "error",
                message = "Failed to clear device memory cache.",
            });
        }

        await Task.CompletedTask;
        return new JsonResult(new
        {
            status = "ok",
        });
    }

    [HttpPost("api/assignment/request/{id}")]
    public async Task<JsonResult> ReQuest(uint id)
    {
        await Task.CompletedTask;
        return new JsonResult(new
        {
            status = "ok",
        });
    }

    [HttpPost("api/device/assign/{uuid}")]
    public async Task<JsonResult> AssignDevice(string uuid, AssignDevicePayload payload)
    {
        await Task.CompletedTask;
        return new JsonResult(new
        {
            status = "ok",
        });
    }

    // TODO: Start Assignment/AssignmentGroup
}

public class FortDetailsPayload
{
    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;
}

public class AssignDevicePayload
{
    public string InstanceName { get; set; } = null!;
}