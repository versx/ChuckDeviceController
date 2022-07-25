namespace ChuckDeviceController.Pvp.GameMaster
{
    public class PokemonSettings
    {
        public string? PokemonId { get; set; }

        public string? Form { get; set; }

        public PokemonBaseIvStats? Stats { get; set; }

        public double PokedexHeightM { get; set; }

        public double PokedexWeightKg { get; set; }

        public List<PokemonEvolutionBranch>? EvolutionBranch { get; set; }

        public List<string>? ObCostumeEvolution { get; set; }
    }
}