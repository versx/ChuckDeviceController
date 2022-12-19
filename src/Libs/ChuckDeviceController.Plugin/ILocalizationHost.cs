namespace ChuckDeviceController.Plugin;

using System.Globalization;

/// <summary>
/// Plugin host handler contract used to translate strings.
/// </summary>
public interface ILocalizationHost
{
    #region Properties

    /// <summary>
    /// Gets or sets the current culture localization to use.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the two letter ISO country code for the currently set localization.
    /// </summary>
    /// <value>The two letter ISO country code.</value>
    string CountryCode { get; }

    #endregion

    /// <summary>
    /// Sets the country locale code to use for translations.
    /// </summary>
    /// <param name="locale">Two letter ISO language name code.</param>
    void SetLocale(string locale);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    string Translate(string key);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyWithArgs"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    string Translate(string keyWithArgs, params object[] args);

    /// <summary>
    /// Translate a Pokemon id to name.
    /// </summary>
    /// <param name="pokemonId">Pokemon ID to translate to name.</param>
    /// <returns></returns>
    string GetPokemonName(uint pokemonId);

    /// <summary>
    /// Translate a Pokemon form id to name.
    /// </summary>
    /// <param name="formId">Form ID to translate to name.</param>
    /// <param name="includeNormal">Include 'Normal' form name or not.</param>
    /// <returns></returns>
    string GetFormName(uint formId, bool includeNormal = false);

    /// <summary>
    /// Translate a Pokemon costume id to name.
    /// </summary>
    /// <param name="costumeId">Costume ID to translate to name.</param>
    /// <returns></returns>
    string GetCostumeName(uint costumeId);

    /// <summary>
    /// Translate a Pokemon evolution id to name.
    /// </summary>
    /// <param name="evolutionId">Evolution ID to translate to name.</param>
    /// <returns></returns>
    string GetEvolutionName(uint evolutionId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="moveId"></param>
    /// <returns></returns>
    string GetMoveName(uint moveId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="throwTypeId"></param>
    /// <returns></returns>
    string GetThrowName(uint throwTypeId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns></returns>
    string GetItem(uint itemId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="weatherConditionId"></param>
    /// <returns></returns>
    string GetWeather(uint weatherConditionId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="alignmentTypeId"></param>
    /// <returns></returns>
    string GetAlignmentName(uint alignmentTypeId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="characterCategoryId"></param>
    /// <returns></returns>
    string GetCharacterCategoryName(uint characterCategoryId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="invasionCharacterId"></param>
    /// <returns></returns>
    string GetGruntType(uint invasionCharacterId);
}