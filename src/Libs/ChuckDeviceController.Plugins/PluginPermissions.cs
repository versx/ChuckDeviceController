namespace ChuckDeviceController.Plugin
{
    /// <summary>
    /// Enumeration of available permissions a plugin can request.
    /// </summary>
    [Flags]
    public enum PluginPermissions : byte
    {
        /// <summary>
        /// No extra permissions
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Read database entities
        /// </summary>
        ReadDatabase = 0x1,

        /// <summary>
        /// Write database entities
        /// </summary>
        WriteDatabase = 0x2,
        
        /// <summary>
        /// Delete database entities (NOTE: Should probably remove since Delete == Write essentially but would be nice to separate it)
        /// </summary>
        DeleteDatabase = 0x4,

        /// <summary>
        /// Add new ASP.NET Mvc controller routes
        /// </summary>
        AddControllers = 0x8,

        /// <summary>
        /// Add new job controller instances for devices
        /// </summary>
        AddJobControllers = 0x10,

        /// <summary>
        /// Add new instances
        /// </summary>
        AddInstances = 0x20,

        /// <summary>
        /// All available permissions
        /// </summary>
        All = ReadDatabase |
              WriteDatabase |
              DeleteDatabase |
              AddControllers |
              AddJobControllers |
              AddInstances,
    }
}