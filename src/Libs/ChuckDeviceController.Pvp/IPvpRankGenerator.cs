namespace ChuckDeviceController.Pvp;

using ChuckDeviceController.Pvp.Models;

using POGOProtos.Rpc;

using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

public interface IPvpRankGenerator
{
    Task InitializeAsync();

    /// <summary>
    /// Generate all possible PVP rankings for all PVP leagues for a specific Pokemon.
    /// </summary>
    /// <param name="pokemon">Pokemon ID to generate rankings for.</param>
    /// <param name="form">Pokemon form</param>
    /// <param name="gender">Pokemon gender</param>
    /// <param name="costume">Pokemon costume</param>
    /// <param name="iv">Pokemon individual values.</param>
    /// <param name="level">Minimum Pokemon level.</param>
    /// <returns>Returns a dictionary of all possible PVP league rankings for the Pokemon.</returns>
    IReadOnlyDictionary<string, dynamic>? GetAllPvpLeagues(
        HoloPokemonId pokemon,
        PokemonForm? form,
        PokemonGender? gender,
        PokemonCostume? costume,
        IV iv,
        double level);

    IReadOnlyList<PvpRank> GetPvpStats(
        HoloPokemonId pokemon,
        PokemonForm? form,
        IV iv,
        double level,
        PvpLeague league);

    IReadOnlyList<PvpRank>? GetTopPvpRanks(
        HoloPokemonId pokemon,
        PokemonForm? form,
        PvpLeague league);
}