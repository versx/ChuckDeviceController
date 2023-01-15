namespace ChuckDeviceConfigurator.Controllers;

using System.Net.Mime;

using Microsoft.AspNetCore.Mvc;

using ChuckDeviceController.Data.Extensions;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Plugin;

[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public class HelperController : ControllerBase
{
    private readonly IUnitOfWork _uow;
    private readonly IUiHost _uiHost;

    public HelperController(
        IUnitOfWork uow,
        IUiHost uiHost)
    {
        _uow = uow;
        _uiHost = uiHost;
    }

    [HttpGet("GetSidebarItems")]
    public IActionResult GetSidebarItems()
    {
        return new JsonResult(_uiHost.SidebarItems);
    }

    //[HttpGet("GetSettingsProperties")]
    //public IActionResult GetSettingsProperties()
    //{
    //    foreach (var (key, value) in _uiHost.SettingsProperties)
    //    {
    //        var properties = _uiHost.SettingsProperties[key];
    //        var grouped = properties.GroupBy(g => g.Group, g => g, (group, settings) => new
    //        {
    //            Group = group,
    //            Settings = settings,
    //        });
    //    }

    //    return new JsonResult(_uiHost.SettingsProperties);
    //}

    [HttpGet("GetGeofenceData")]
    public async Task<IActionResult> GetGeofenceData(string name)
    {
        // Called from geofence generator when 'Import' button is triggered
        var geofence = await _uow.Geofences.FindByIdAsync(name);
        if (geofence == null)
        {
            return null;
        }

        var type = Convert.ToInt32(geofence.Type);
        var geofenceData = geofence.ConvertToIni();
        return new JsonResult(new
        {
            type,
            geofence = geofenceData,
        });
    }
}