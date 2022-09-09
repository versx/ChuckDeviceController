namespace ChuckDeviceController.Plugin
{
    public interface INotifierHost
    {
        Task SendToAsync(string pluginName);

        Task SendToAllAsync();
    }
}
/*
 * - Plugin sends message
 * - Host receives message
 * - Host sends message to designated plugin or all plugins (maybe?) and possibly plugin <--> host communication (notify.SendToHost, pluginHost.SendToPlugin)
 */