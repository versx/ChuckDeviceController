namespace ChuckDeviceController.PluginManager;

using ChuckDeviceController.Plugin;

public sealed class PluginEventHandlers
{
    public IDatabaseEvents? DatabaseEvents { get; set; }

    public IUiEvents? UiEvents { get; set; }

    public IJobControllerServiceEvents? JobControllerEvents { get; set; }

    public ISettingsPropertyEvents? SettingsEvents { get; set; }

    public PluginEventHandlers()
    {
    }

    public PluginEventHandlers(
        IDatabaseEvents databaseEvents,
        IUiEvents uiEvents,
        IJobControllerServiceEvents jobControllerEvents,
        ISettingsPropertyEvents settingsEvents)
    {
        DatabaseEvents = databaseEvents;
        UiEvents = uiEvents;
        JobControllerEvents = jobControllerEvents;
        SettingsEvents = settingsEvents;
    }
}