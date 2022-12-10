namespace ChuckDeviceController.JobControllers.Models
{
    using ChuckDeviceController.Data.Entities;

    public class PokestopWithMode
    {
        public Pokestop Pokestop { get; set; }

        public bool IsAlternative { get; set; }

        public PokestopWithMode(Pokestop pokestop, bool isAlternative)
        {
            Pokestop = pokestop;
            IsAlternative = isAlternative;
        }
    }
}