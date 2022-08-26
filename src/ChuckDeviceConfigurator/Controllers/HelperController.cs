﻿namespace ChuckDeviceConfigurator.Controllers
{
    using System.Net.Mime;

    using Microsoft.AspNetCore.Mvc;

    using ChuckDeviceController.Plugin;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    public class HelperController : ControllerBase
    {
        private const string DefaultTheme = "light";

        //private readonly ILogger<HelperController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUiHost _uiHost;

        public HelperController(
            //ILogger<HelperController> logger,
            IConfiguration configuration,
            IUiHost uiHost)
        {
            //_logger = logger;
            _configuration = configuration;
            _uiHost = uiHost;
        }

        [HttpGet("GetNavbarHeaders")]
        public IActionResult GetNavbarHeaders()
        {
            return new JsonResult(_uiHost.NavbarHeaders);
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
            return new JsonResult(_uiHost.SettingsProperties);
        }
    }
}
