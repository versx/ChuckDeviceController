namespace ChuckDeviceController.Data.Common;

using System.ComponentModel.DataAnnotations;

[Flags]
public enum PluginApiKeyScope : int
{
    /// <summary>
    /// No extra permissions
    /// </summary>
    [Display(GroupName = "General", Name = "None", Description = "No permissions")]
    None = 0, // 000000  0

    #region Database

    /// <summary>
    /// Read database entities
    /// </summary>
    [Display(GroupName = "Database", Name = "Read", Description = "Read data from the database")]
    ReadDatabase = 1 << 0, // 000001  1

    /// <summary>
    /// Write database entities
    /// </summary>
    [Display(GroupName = "Database", Name = "Write", Description = "Write data to the database")]
    WriteDatabase = 1 << 1, // 000010  2

    /// <summary>
    /// Delete database entities (NOTE: Should probably remove since Delete == Write essentially but would be nice to separate it)
    /// </summary>
    [Display(GroupName = "Database", Name = "Delete", Description = "Delete data from the database")]
    DeleteDatabase = 1 << 2, // 000100  4

    #endregion

    /// <summary>
    /// Create new ASP.NET Mvc controller routes
    /// </summary>
    [Display(GroupName = "Controllers", Name = "Create", Description = "Create new MVC controllers")]
    CreateControllers = 1 << 3, // 001000  8

    /// <summary>
    /// Create new job controller types that devices can use
    /// </summary>
    [Display(GroupName = "JobControllers", Name = "Create", Description = "Create new job controllers")]
    CreateJobControllers = 1 << 4, // 010000  16

    #region Instances

    /// <summary>
    /// Create new instances
    /// </summary>
    [Display(GroupName = "Instances", Name = "Create", Description = "Create new instances")]
    CreateInstances = 1 << 5, // 100000  32

    /// <summary>
    /// Create new instances
    /// </summary>
    [Display(GroupName = "Instances", Name = "Edit", Description = "Edit existing instances")]
    EditInstances = 1 << 6, // 100000  64

    /// <summary>
    /// Create new instances
    /// </summary>
    [Display(GroupName = "Instances", Name = "Delete", Description = "Delete existing instances")]
    DeleteInstances = 1 << 7, // 100000  128

    #endregion

    #region Geofences

    /// <summary>
    /// Create new geofences
    /// </summary>
    [Display(GroupName = "Geofences", Name = "Create", Description = "Create new geofences")]
    CreateGeofences = 1 << 8, // 1000000 256

    /// <summary>
    /// Edit existing geofences
    /// </summary>
    [Display(GroupName = "Geofences", Name = "Edit", Description = "Edit existing geofences")]
    EditGeofences = 1 << 9, // 1000000 512

    /// <summary>
    /// Delete existing geofences
    /// </summary>
    [Display(GroupName = "Geofences", Name = "Delete", Description = "Delete existing geofences")]
    DeleteGeofences = 1 << 10, // 1000000 1024

    #endregion

    #region File System

    /// <summary>
    /// Read file system storage
    /// </summary>
    [Display(GroupName = "FileSystem", Name = "Read", Description = "Read from the file system")]
    ReadFileSystem = 1 << 11, // 10000000  2048

    /// <summary>
    /// Write file system storage
    /// </summary>
    [Display(GroupName = "FileSystem", Name = "Write", Description = "Write to the file system")]
    WriteFileSystem = 1 << 12, // 100000000  4096

    #endregion

    #region Devices

    /// <summary>
    /// Assign devices to job controller instances
    /// </summary>
    [Display(GroupName = "Devices", Name = "Assign", Description = "Assign devices to job controller instances")]
    AssignDevices = 1 << 13, // 1000000000  8192

    /// <summary>
    /// Create new devices
    /// </summary>
    [Display(GroupName = "Devices", Name = "Create", Description = "Create new devices")]
    CreateDevices = 1 << 14, // 100000000000000  16392

    /// <summary>
    /// Edit existing devices
    /// </summary>
    [Display(GroupName = "Devices", Name = "Edit", Description = "Edit existing devices")]
    EditDevices = 1 << 15, // 1000000000000000  32784

    /// <summary>
    /// Delete existing devices
    /// </summary>
    [Display(GroupName = "Devices", Name = "Delete", Description = "Delete existing devices")]
    DeleteDevices = 1 << 16, // 10000000000000000  65568

    #endregion

    /// <summary>
    /// All permissions
    /// </summary>
    [Display(GroupName = "General", Name = "All", Description = "All permissions")]
    //All = ~(~0 << 17), // 111111111  511 | 111  7
    All = // 11111111111111111  131071
        ReadDatabase |
        WriteDatabase |
        DeleteDatabase |
        CreateControllers |
        CreateJobControllers |
        CreateInstances |
        EditInstances |
        DeleteInstances |
        CreateGeofences |
        EditGeofences |
        DeleteGeofences |
        ReadFileSystem |
        WriteFileSystem |
        AssignDevices |
        CreateDevices |
        EditDevices |
        DeleteDevices,
}