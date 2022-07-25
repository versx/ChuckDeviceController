namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    public class IvQueueViewModel
    {
        public string? Name { get; set; }

        public List<IvQueueItemViewModel> Queue { get; set; } = new();

        public bool AutoRefresh { get; set; }
    }

    public class IvQueueItemViewModel
    {
        [DisplayName("#")]
        public uint Index { get; set; }

        [DisplayName("Image")]
        public string? Image { get; set; }

        [DisplayName("ID")]
        public uint PokemonId { get; set; }

        [DisplayName("Name")]
        public string? PokemonName { get; set; }

        [DisplayName("Encounter ID")]
        public string? EncounterId { get; set; }

        [DisplayName("Location")]
        public string? Location { get; set; }
    }
}