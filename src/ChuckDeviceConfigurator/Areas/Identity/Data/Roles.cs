namespace ChuckDeviceConfigurator.Data
{
    public enum Roles
    {
        /// <summary>
        /// Default root account, full access.
        /// </summary>
        SuperAdmin, // root - full access

        /// <summary>
        /// 
        /// </summary>
        Admin, // Users & roles maybe?

        /// <summary>
        /// No access other than to login, front dashboard,
        /// and manage account pages.
        /// </summary>
        Registered,

        /// <summary>
        /// Access to Accounts management pages.
        /// </summary>
        Accounts,

        /// <summary>
        /// Access to Assignments management pages.
        /// </summary>
        Assignments,

        /// <summary>
        /// Access to ASsignment Groups management pages.
        /// </summary>
        AssignmentGroups,

        /// <summary>
        /// Access to Devices management pages.
        /// </summary>
        Devices,

        /// <summary>
        /// Access to Device Groups management pages.
        /// </summary>
        DeviceGroups,

        /// <summary>
        /// Access to Geofences management pages.
        /// </summary>
        Geofences,

        /// <summary>
        /// Access to Instances management pages.
        /// </summary>
        Instances,

        /// <summary>
        /// Access to IV lists management pages.
        /// </summary>
        IvLists,

        /// <summary>
        /// Access to Webhooks management pages.
        /// </summary>
        Webhooks,

        /// <summary>
        /// Access to Utiliies page.
        /// </summary>
        Utilities,
    }
}