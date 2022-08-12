namespace ChuckDeviceConfigurator.Controllers
{
    using System.Net.Mime;

    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Plugins;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class HelperController : ControllerBase
    {
        private const string DefaultTheme = "light";

        private readonly ILogger<HelperController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUiHost _uiHost;

        public HelperController(
            ILogger<HelperController> logger,
            IConfiguration configuration,
            IUiHost uiHost)
        {
            _logger = logger;
            _configuration = configuration;
            _uiHost = uiHost;
        }

        [HttpGet("GetNavbarHeaders")]
        public IActionResult GetNavbarHeaders()
        {
            // Get cached navbar headers from plugins
            return new JsonResult(_uiHost.NavbarHeaders);
        }

        [HttpGet("GetTheme")]
        public IActionResult GetTheme()
        {
            var theme = _configuration.GetValue<string>("Theme") ?? DefaultTheme;
            return new JsonResult(theme);
        }
    }
}
