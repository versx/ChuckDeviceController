namespace ChuckDeviceConfigurator.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceConfigurator.Services.Jobs;
    using ChuckDeviceController.Common.Jobs;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Geometry.Models;

    [ApiController]
    public class ApiController : ControllerBase
    {
        #region Variables

        private readonly ILogger<IJobControllerService> _logger;
        private readonly IJobControllerService _jobControllerService;
        private readonly ControllerDbContext _controllerContext;

        #endregion

        #region Constructor

        public ApiController(
            ILogger<IJobControllerService> logger,
            IJobControllerService jobControllerService,
            ControllerDbContext controllerContext)
        {
            _logger = logger;
            _jobControllerService = jobControllerService;
            _controllerContext = controllerContext;
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
    }
}