namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    using ChuckDeviceController.Data.Entities;

    public class ConvertFortsViewModel
    {
        [DisplayName("Pokestops -> Gyms")]
        public List<Pokestop> PokestopsToGyms { get; set; } = new();

        [DisplayName("Gyms -> Pokestops")]
        public List<Gym> GymsToPokestops { get; set; } = new();
    }
}