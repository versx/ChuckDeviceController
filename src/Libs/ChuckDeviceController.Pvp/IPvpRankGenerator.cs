namespace ChuckDeviceController.Pvp
{
    using ChuckDeviceController.Pvp.Models;

    using POGOProtos.Rpc;

    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

    public interface IPvpRankGenerator
    {
        Dictionary<string, dynamic>? GetAllPvpLeagues(HoloPokemonId pokemon, PokemonForm form, PokemonGender gender, PokemonCostume costume, IV iv, double level);

        List<PvpRank> GetPvpStats(HoloPokemonId pokemon, PokemonForm form, IV iv, double level, PvpLeague league);

        List<PvpRank> GetTopPvpRanks(HoloPokemonId pokemon, PokemonForm form, PvpLeague league);
    }
}