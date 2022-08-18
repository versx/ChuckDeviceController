namespace ChuckDeviceController.PluginManager
{
    using ChuckDeviceController.Plugins;

    public sealed class PluginEventHandlers
    {
        public IDatabaseEvents? DatabaseEvents { get; set; }

        public IUiEvents? UiEvents { get; set; }

        public IJobControllerServiceEvents? JobControllerEvents { get; set; }

        public PluginEventHandlers()
        {
        }

        public PluginEventHandlers(
            IDatabaseEvents databaseEvents,
            IUiEvents uiEvents,
            IJobControllerServiceEvents jobControllerEvents)
        {
            DatabaseEvents = databaseEvents;
            UiEvents = uiEvents;
            JobControllerEvents = jobControllerEvents;
        }
    }
}