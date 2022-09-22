namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    using ChuckDeviceController.Plugin;

    public class DashboardViewModel
    {
        // Controller counts
        [
            DisplayName("Accounts"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Accounts { get; set; }

        [
            DisplayName("Assignments"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Assignments { get; set; }

        [
            DisplayName("Assignment Groups"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong AssignmentGroups { get; set; }

        [
            DisplayName("Devices"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Devices { get; set; }

        [
            DisplayName("Device Groups"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong DeviceGroups { get; set; }

        [
            DisplayName("Geofences"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Geofences { get; set; }

        [
            DisplayName("Instances"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Instances { get; set; }

        [
            DisplayName("IV Lists"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong IvLists { get; set; }

        [
            DisplayName("Plugins"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Plugins { get; set; }

        [
            DisplayName("Users"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Users { get; set; }

        [
            DisplayName("Webhooks"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Webhooks { get; set; }

        // Map data counts
        [
            DisplayName("Gyms"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Gyms { get; set; }

        [
            DisplayName("Gym Defenders"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong GymDefenders { get; set; }

        [
            DisplayName("Gym Trainers"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong GymTrainers { get; set; }

        [
            DisplayName("Raids"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Raids { get; set; }

        [
            DisplayName("Pokemon"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Pokemon { get; set; }

        [
            DisplayName("Pokestops"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Pokestops { get; set; }

        [
            DisplayName("Lures"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Lures { get; set; }

        [
            DisplayName("Pokestop Incidents"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Incidents { get; set; }

        [
            DisplayName("Quests"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Quests { get; set; }

        [
            DisplayName("S2 Cells"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Cells { get; set; }

        [
            DisplayName("Spawnpoints"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Spawnpoints { get; set; }

        [
            DisplayName("Weather Cells"),
            DisplayFormat(DataFormatString = "{0:N0}"),
        ]
        public ulong Weather { get; set; }

        [DisplayName("Uptime")]
        public string? Uptime { get; set; }

        [DisplayName("Started")]
        public string? Started { get; set; }

        public IReadOnlyList<IDashboardStatsItem> PluginDashboardStats { get; set; }
    }
}