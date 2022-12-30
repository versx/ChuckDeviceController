namespace ChuckDeviceController.Common;

using System.ComponentModel.DataAnnotations;

public enum Roles
{
    /// <summary>
    /// Default root account, full access.
    /// </summary>
    [Display(GroupName = "Administration", Name = "Super Admin", Description = "")]
    SuperAdmin, // root - full access

    /// <summary>
    /// 
    /// </summary>
    [Display(GroupName = "Administration", Name = "Admin", Description = "")]
    Admin, // Users & roles maybe?

    /// <summary>
    /// No access other than to login, front dashboard,
    /// and manage account pages.
    /// </summary>
    [Display(GroupName = "", Name = "Registered", Description = "")]
    Registered,

    /// <summary>
    /// Access to Accounts management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Accounts", Description = "")]
    Accounts,

    /// <summary>
    /// 
    /// </summary>
    [Display(GroupName = "", Name = "API Keys", Description = "")]
    ApiKeys,

    /// <summary>
    /// Access to Assignments management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Assignments", Description = "")]
    Assignments,

    /// <summary>
    /// Access to Assignment Groups management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Assignment Groups", Description = "")]
    AssignmentGroups,

    /// <summary>
    /// Access to Benchmark pages.
    /// </summary>
    [Display(GroupName = "", Name = "Benchmarks", Description = "")]
    Benchmarks,

    /// <summary>
    /// Access to Devices management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Devices", Description = "")]
    Devices,

    /// <summary>
    /// Access to Device Groups management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Device Groups", Description = "")]
    DeviceGroups,

    /// <summary>
    /// Access to Geofences management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Geofences", Description = "")]
    Geofences,

    /// <summary>
    /// Access to Instances management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Instances", Description = "")]
    Instances,

    /// <summary>
    /// Access to IV lists management pages.
    /// </summary>
    [Display(GroupName = "", Name = "IV lists", Description = "")]
    IvLists,

    /// <summary>
    /// Access to Plugin management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Plugins", Description = "")]
    Plugins,

    /// <summary>
    /// Access to Webhooks management pages.
    /// </summary>
    [Display(GroupName = "", Name = "Webhooks", Description = "")]
    Webhooks,

    /// <summary>
    /// Access to Utiliies page.
    /// </summary>
    [Display(GroupName = "", Name = "Utilities", Description = "")]
    Utilities,
}