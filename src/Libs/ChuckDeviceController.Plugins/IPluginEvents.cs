namespace ChuckDeviceController.Plugins
{
    using ChuckDeviceController.Common.Data;

    /// <summary>
    /// Provides delegates of plugin related events
    /// from the host application.
    /// </summary>
    public interface IPluginEvents
    {
        /// <summary>
        /// Called when the plugin has been fully loaded
        /// and initialized from the host application.
        /// </summary>
        void OnLoad();

        /// <summary>
        /// Called when the plugin has been reloaded
        /// by the host application.
        /// </summary>
        void OnReload();

        /// <summary>
        /// Called when the plugin has been stopped by
        /// the host application.
        /// </summary>
        void OnStop();

        /// <summary>
        /// Called when the plugin has been removed by
        /// the host application.
        /// </summary>
        void OnRemove();

        /// <summary>
        /// Called when the plugin's state has been
        /// changed by the host application.
        /// </summary>
        /// <param name="state">Plugin's current state</param>
        void OnStateChanged(PluginState state);
    }
}