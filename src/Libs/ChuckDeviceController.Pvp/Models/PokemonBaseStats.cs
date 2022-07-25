namespace ChuckDeviceController.Pvp.Models
{
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;

    public class PokemonBaseStats
    {
        public ushort BaseAttack { get; set; }

        public ushort BaseDefense { get; set; }

        public ushort BaseStamina { get; set; }

        public List<PokemonWithFormAndGender> Evolutions { get; set; } = new();

        public double BaseHeight { get; set; }

        public double BaseWeight { get; set; }

        public List<PokemonCostume>? CostumeEvolutionOverride { get; set; }
    }
}