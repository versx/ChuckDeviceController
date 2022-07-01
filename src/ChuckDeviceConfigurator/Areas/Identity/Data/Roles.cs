namespace ChuckDeviceConfigurator.Data
{
    public enum Roles
    {
        SuperAdmin, // root - full access
        Admin, // Users & roles maybe?
        Moderator, // Unused - not needed
        Registered, // No access other than login and front dashboard page

        // Controller data models
        Accounts,
        Assignments,
        Devices,
        Geofences,
        Instances,
        IvLists,
        Webhooks,
    }
}