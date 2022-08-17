namespace ChuckDeviceController.Plugins
{
    // TODO: Include [Authorize(Roles = RoleConsts.*)] role check for controllers

    // NOTE: Only used for DI testing purposes
    public interface IPluginService
    {
        string Test { get; }
    }

    /// <summary>
    /// Base Plugin interface contract all plugins will inherit
    /// at a minimum.
    /// </summary>
    public interface IPlugin : IMetadata, IPluginEvents, IWebPlugin // REVIEW: Possibly make 'IWebPlugin' optional
    {
    }
}