namespace ChuckDeviceController.Common.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PluginState
    {
        /// <summary>
        /// Plugin state has not be set yet
        /// </summary>
        Unset = 0,

        /// <summary>
        /// Plugin is currently running and active
        /// </summary>
        Running,

        /// <summary>
        /// Plugin has been stopped and is not currently running
        /// </summary>
        Stopped,

        /// <summary>
        /// Plugin has been disabled and is not curretly running
        /// or enabled
        /// </summary>
        Disabled,

        /// <summary>
        /// Plugin has been removed from the host application
        /// and is no longer available
        /// </summary>
        Removed,

        /// <summary>
        /// Plugin has encountered an error and unable to recover
        /// </summary>
        Error,
    }
}