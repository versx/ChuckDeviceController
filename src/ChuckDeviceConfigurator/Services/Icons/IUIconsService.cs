namespace ChuckDeviceConfigurator.Services.Icons
{
    using static POGOProtos.Rpc.PokemonDisplayProto.Types;

    public interface IUIconsService
    {
        string GetPokemonIcon(uint pokemonId, uint formId = 0, uint evolutionId = 0, Gender gender = 0, uint costumeId = 0, bool shiny = false);
    }
}