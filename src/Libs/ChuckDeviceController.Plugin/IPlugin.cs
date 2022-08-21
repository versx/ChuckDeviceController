namespace ChuckDeviceController.Plugin
{
    // TODO: Include [Authorize(Roles = RoleConsts.*)] role check for controllers

    /// <summary>
    /// Base Plugin interface contract all plugins are required to
    /// inherit at a minimum.
    /// </summary>
    public interface IPlugin : IMetadata, IWebPlugin, IPluginEvents // REVIEW: Possibly make 'IWebPlugin' optional
    {
    }
}