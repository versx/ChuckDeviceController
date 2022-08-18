namespace ChuckDeviceController.PluginManager.Services.Loader
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PluginLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public IPluginHost PluginHost { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pluginHost"></param>
        public PluginLoadedEventArgs(IPluginHost pluginHost)
        {
            PluginHost = pluginHost;
        }
    }
}