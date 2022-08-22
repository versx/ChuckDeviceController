﻿namespace ChuckDeviceController.Common
{
    public static class RoleConsts
    {
        public const string DefaultRole = nameof(Roles.Registered);

        public const string AccountsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Accounts)}";

        public const string AssignmentsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Assignments)}";

        public const string AssignmentGroupsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.AssignmentGroups)}";

        public const string DevicesRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Devices)}";

        public const string DeviceGroupsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.DeviceGroups)}";

        public const string GeofencesRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Geofences)}";

        public const string InstancesRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Instances)}";

        public const string IvListsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.IvLists)}";

        public const string PluginsRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Plugins)}";

        public const string WebhooksRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Webhooks)}";

        public const string UsersRole = nameof(Roles.SuperAdmin);

        public const string UtilitiesRole = $"{nameof(Roles.SuperAdmin)},{nameof(Roles.Admin)},{nameof(Roles.Utilities)}";

        public const string AdminRole = nameof(Roles.Admin);

        public const string SuperAdminRole = nameof(Roles.SuperAdmin);
    }
}