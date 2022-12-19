namespace ChuckDeviceConfigurator.Localization;

using System.Globalization;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TFrom"></typeparam>
/// <typeparam name="TTo"></typeparam>
/// <typeparam name="TDictionary"></typeparam>
public interface ILanguage<TFrom, TTo, TDictionary> : IEnumerable<KeyValuePair<TFrom, TTo>>
    where TDictionary : IDictionary<TFrom, TTo>
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

    /// <summary>
    /// Gets or sets the locale directory.
    /// </summary>
    /// <value>The locale directory.</value>
    string LocaleDirectory { get; }

    /// <summary>
    /// Property to get/set the default to value if a lookup fails
    /// </summary>
    /// <value>The default value.</value>
    TTo? DefaultValue { get; }

    /// <summary>
    /// Property that returns the number of elements in the lookup table
    /// </summary>
    /// <value>The translation count.</value>
    int TranslationCount { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Sets the current locale.
    /// </summary>
    /// <param name="locale">Two letter ISO language name code.</param>
    void SetLocale(string locale);

    /// <summary>
    /// Performs a translation using the table, returns the default from value
    /// if cannot find a matching result.
    /// </summary>
    /// <returns>The translate.</returns>
    /// <param name="value">Value.</param>
    TTo Translate(TFrom value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    void Add(TFrom name, TTo value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    bool Exists(TFrom name);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    bool Remove(TFrom name);

    /// <summary>
    /// Clears all existing translations and defaults
    /// </summary>
    void Clear();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="languageName"></param>
    /// <returns></returns>
    string GetTwoLetterIsoLanguageName(string languageName);

    #endregion
}