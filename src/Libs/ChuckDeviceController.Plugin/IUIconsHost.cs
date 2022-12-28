namespace ChuckDeviceController.Plugin;

/// <summary>
/// UIcons standard host handler to retrieve icon url endpoints for plugins.
/// </summary>
public interface IUIconsHost
{
    /// <summary>
    /// Gets an icon image url based on the provided Pokemon details.
    /// </summary>
    /// <param name="pokemonId">Pokemon pokedex id.</param>
    /// <param name="formId">Pokemon form id.</param>
    /// <param name="evolutionId">Pokemon mega evolution id.</param>
    /// <param name="gender">Pokemon gender id.</param>
    /// <param name="costumeId">Pokemon costume id.</param>
    /// <param name="shiny">Whether the Pokemon is shiny or not.</param>
    /// <returns>Returns a url of the Pokemon image.</returns>
    string GetPokemonIcon(uint pokemonId, uint formId = 0, uint evolutionId = 0, uint gender = 0, uint costumeId = 0, bool shiny = false);
}