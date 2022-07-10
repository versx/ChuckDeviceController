namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    public class DashboardViewModel
    {
        // Controller counts
        [DisplayName("Accounts")]
        public uint Accounts { get; set; }

        [DisplayName("Assignments")]
        public uint Assignments { get; set; }

        [DisplayName("Devices")]
        public uint Devices { get; set; }

        [DisplayName("Device Groups")]
        public uint DeviceGroups { get; set; }

        [DisplayName("Geofences")]
        public uint Geofences { get; set; }

        [DisplayName("Instances")]
        public uint Instances { get; set; }

        [DisplayName("IV Lists")]
        public uint IvLists { get; set; }

        [DisplayName("Users")]
        public uint Users { get; set; }

        [DisplayName("Webhooks")]
        public uint Webhooks { get; set; }

        // Map data counts
        [DisplayName("Gyms")]
        public uint Gyms { get; set; }

        [DisplayName("Gym Defenders")]
        public uint GymDefenders { get; set; }

        [DisplayName("Gym Trainers")]
        public uint GymTrainers { get; set; }

        [DisplayName("Pokemon")]
        public uint Pokemon { get; set; }

        [DisplayName("Pokestops")]
        public uint Pokestops { get; set; }

        [DisplayName("Pokestop Incidents")]
        public uint Incidents { get; set; }

        [DisplayName("S2 Cells")]
        public uint Cells { get; set; }

        [DisplayName("Spawnpoints")]
        public uint Spawnpoints { get; set; }

        [DisplayName("Weather")]
        public uint Weather { get; set; }
    }
}