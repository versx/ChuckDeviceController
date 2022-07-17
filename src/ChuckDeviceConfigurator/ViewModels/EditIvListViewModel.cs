namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    public class EditIvListViewModel
    {
        [DisplayName("Name")]
        public string Name { get; set; }

        [DisplayName("Pokemon IDs")]
        public string PokemonIds { get; set; }
    }
}