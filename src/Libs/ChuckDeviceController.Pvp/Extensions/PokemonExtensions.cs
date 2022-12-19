namespace ChuckDeviceController.Pvp.Extensions;

using POGOProtos.Rpc;
using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

using Enum = System.Enum;

public static class PokemonExtensions
{
    public static HoloPokemonId GetPokemonFromName(this string name)
    {
        var allPokemon = new List<HoloPokemonId>(Enum.GetValues<HoloPokemonId>());
        var pokemon = GetEnumFromName(name, allPokemon);
        return pokemon;
    }

    public static PokemonForm? GetFormFromName(this string name)
    {
        var allForms = new List<PokemonForm>(Enum.GetValues<PokemonForm>());
        var form = GetEnumFromName(name, allForms);
        return form;
    }

    public static PokemonGender? GetGenderFromName(this string name)
    {
        var allGenders = new List<PokemonGender>(Enum.GetValues<PokemonGender>());
        var gender = GetEnumFromName(name, allGenders);
        return gender;
    }

    public static PokemonCostume? GetCostumeFromName(this string name)
    {
        var allCostumes = new List<PokemonCostume>(Enum.GetValues<PokemonCostume>());
        var costume = GetEnumFromName(name, allCostumes);
        return costume;
    }

    private static T? GetEnumFromName<T>(string name, List<T> values)
    {
        var lowerName = name.Replace("_", "").ToLower();
        var result = values.FirstOrDefault(x => x?.ToString()?.ToLower() == lowerName);
        return result;
    }
}