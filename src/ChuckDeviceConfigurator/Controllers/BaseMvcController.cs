namespace ChuckDeviceConfigurator.Controllers;

using Microsoft.AspNetCore.Mvc;

using ChuckDeviceConfigurator.ViewModels;
using ChuckDeviceController.Extensions.Json;

public class BaseMvcController : Controller
{
    [NonAction]
    public void CreateNotification(NotificationViewModel notification)
    {
        TempData.TryGetValue("Notifications", out var value);
        var notifications = value as List<NotificationViewModel> ?? new();
        notifications.Add(notification);
        var json = notifications.ToJson();
        TempData["Notifications"] = json;
    }

    [HttpGet]
    public PartialViewResult Notifications()
    {
        TempData.TryGetValue("Notifications", out var json);
        var notifications = json?.ToString()?.FromJson<List<NotificationViewModel>>() ?? new();
        return PartialView("_NotificationsPartial", notifications);
    }

    [NonAction]
    public void CreateSuccessNotification(string message)
    {
        CreateNotification(new NotificationViewModel
        {
            Message = message,
            Icon = NotificationIcon.Success,
        });
    }

    [NonAction]
    public void CreateErrorNotification(string message)
    {
        CreateNotification(new NotificationViewModel
        {
            Message = message,
            Icon = NotificationIcon.Error,
        });
    }
}