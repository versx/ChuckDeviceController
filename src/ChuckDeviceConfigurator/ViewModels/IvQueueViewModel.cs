namespace ChuckDeviceConfigurator.ViewModels
{
    using System.ComponentModel;

    using static POGOProtos.Rpc.PokemonDisplayProto.Types;

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

        [DisplayName("Form")]
        public string? PokemonForm { get; set; }

        [DisplayName("Form ID")]
        public ushort PokemonFormId { get; set; }

        [DisplayName("Costume")]
        public string? PokemonCostume { get; set; }

        [DisplayName("Costume ID")]
        public ushort PokemonCostumeId { get; set; }

        [DisplayName("Gender")]
        public Gender PokemonGender { get; set; }

        [DisplayName("Encounter ID")]
        public string? EncounterId { get; set; }

        //[DisplayName("Location")]
        //public string? Location { get; set; }

        [DisplayName("Latitude")]
        public double Latitude { get; set; }

        [DisplayName("Longitude")]
        public double Longitude { get; set; }
    }
}