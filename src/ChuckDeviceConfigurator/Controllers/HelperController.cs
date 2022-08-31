namespace ChuckDeviceConfigurator.Controllers
{
    using System.Net.Mime;

    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Common.Data;
    using ControllerContext = ChuckDeviceController.Data.Contexts.ControllerContext;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Plugin;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class HelperController : ControllerBase
    {
        private const string DefaultTheme = "light";

        //private readonly ILogger<HelperController> _logger;
        private readonly ControllerContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUiHost _uiHost;

        public HelperController(
            //ILogger<HelperController> logger,
            ControllerContext context,
            IConfiguration configuration,
            IUiHost uiHost)
        {
            //_logger = logger;
            _context = context;
            _configuration = configuration;
            _uiHost = uiHost;
        }

        [HttpGet("GetSidebarItems")]
        public IActionResult GetSidebarItems()
        {
            return new JsonResult(_uiHost.SidebarItems);
        }

        [HttpGet("GetTheme")]
        public IActionResult GetTheme()
        {
            var theme = _configuration.GetValue<string>("Theme") ?? DefaultTheme;
            return new JsonResult(theme);
        }

        [HttpGet("GetTiles")]
        public IActionResult GetTiles()
        {
            return new JsonResult(_uiHost.DashboardTiles);
        }

        [HttpGet("GetSettingsTabs")]
        public IActionResult GetSettingsTabs()
        {
            return new JsonResult(_uiHost.SettingsTabs);
        }

        [HttpGet("GetSettingsProperties")]
        public IActionResult GetSettingsProperties()
        {
            foreach (var (key, value) in _uiHost.SettingsProperties)
            {
                var properties = _uiHost.SettingsProperties[key];
                var grouped = properties.GroupBy(g => g.Group, g => g, (group, settings) => new
                {
                    Group = group,
                    Settings = settings,
                });
            }

            return new JsonResult(_uiHost.SettingsProperties);
        }

        [HttpGet("GetGeofenceData")]
        public async Task<IActionResult> GetGeofenceData(string name)
        {
            var geofence = await _context.Geofences.FindAsync(name);
            if (geofence == null)
            {
                return null;
            }

            var sb = new System.Text.StringBuilder();
            switch (geofence.Type)
            {
                case GeofenceType.Circle:
                    var coordinates = geofence.ConvertToCoordinates();
                    if (coordinates != null)
                    {
                        sb.AppendLine(string.Join("\n", coordinates.Select(x => $"{x.Latitude},{x.Longitude}")));
                    }
                    break;
                case GeofenceType.Geofence:
                    var (_, coords) = geofence.ConvertToMultiPolygons();
                    if (coords != null)
                    {
                        foreach (var coord in coords)
                        {
                            sb.AppendLine($"[{geofence.Name}]");
                            sb.AppendLine(string.Join("\n", coord.Select(x => $"{x.Latitude},{x.Longitude}")));
                        }
                    }
                    break;
            }
            var text = sb.ToString();
            return new JsonResult(text);
        }
    }
}