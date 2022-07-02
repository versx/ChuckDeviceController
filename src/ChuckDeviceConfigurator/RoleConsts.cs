namespace ChuckDeviceConfigurator
{
    using ChuckDeviceConfigurator.Data;

    public static class RoleConsts
    {
        public const string AccountsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Accounts)}";

        public const string AssignmentsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Assignments)}";

        public const string DevicesRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Devices)}";

        public const string GeofencesRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Geofences)}";

        public const string InstancesRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Instances)}";

        public const string IvListsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.IvLists)}";

        public const string WebhooksRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Webhooks)}";

        public const string UserRolesRole = nameof(Roles.SuperAdmin);

        public const string RoleManagerRole = nameof(Roles.SuperAdmin);
    }
}