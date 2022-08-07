namespace ChuckDeviceController.Plugins
{
    public interface ILocalizationHost
    {
        string Translate(string key);

        string Translate(string keyWithArgs, params object[] args);

        string GetPokemonName(uint pokeId);

        string GetFormName(uint formId, bool includeNormal = false);

        string GetCostumeName(uint costumeId);

        string GetEvolutionName(uint evoId);

        string GetMoveName(uint moveId);

        string GetThrowName(uint throwTypeId);

        string GetItem(uint itemId);

        string GetWeather(uint weatherConditionId);

        string GetAlignmentName(uint alignmentTypeId);

        string GetCharacterCategoryName(uint characterCategoryId);

        string GetGruntType(uint invasionCharacterId);
    }
}