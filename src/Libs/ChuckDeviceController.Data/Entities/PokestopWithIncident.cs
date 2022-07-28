namespace ChuckDeviceController.Data.Entities
{
    public class PokestopWithIncident
    {
        public Pokestop Pokestop { get; set; }

        public Incident Invasion { get; set; }

        public PokestopWithIncident(Pokestop pokestop, Incident invasion)
        {
            Pokestop = pokestop;
            Invasion = invasion;
        }
    }
}