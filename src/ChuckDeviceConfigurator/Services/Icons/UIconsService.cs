namespace ChuckDeviceConfigurator.Services.Icons;

using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;
using ChuckDeviceController.Plugin;

/// <summary>
/// 
/// </summary>
public class UIconsService : IUIconsService, IUIconsHost
{
    private const string DefaultIconFormat = "png";
    private const string IndexJson = "index.json";
    private const string DefaultIconUrl = "https://raw.githubusercontent.com/WatWowMap/wwm-uicons/main/pokemon/";

    #region Variables

    private readonly string _baseIconUrl;
    private HashSet<string> _availableForms = new();

    #endregion

    #region Singleton

    private static IUIconsHost? _instance;
    public static IUIconsHost Instance =>
        _instance ??= new UIconsService(
            DefaultIconUrl,
            DefaultIconFormat
        );

    #endregion

    #region Properties

    /// <summary>
    /// 
    /// </summary>
    public string IconFormat { get; private set; } = DefaultIconFormat;

    #endregion

    #region Constructor(s)

    /// <summary>
    /// 
    /// </summary>
    /// <param name="baseIconUrl"></param>
    /// <param name="iconFormat"></param>
    public UIconsService(string baseIconUrl, string iconFormat = DefaultIconFormat)
    {
        _baseIconUrl = baseIconUrl;
        IconFormat = iconFormat;

        Task.Run(async () => await BuildIndexManifestAsync()).Wait();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pokemonId"></param>
    /// <param name="formId"></param>
    /// <param name="evolutionId"></param>
    /// <param name="gender"></param>
    /// <param name="costumeId"></param>
    /// <param name="shiny"></param>
    /// <returns></returns>
    public string GetPokemonIcon(uint pokemonId, uint formId = 0, uint evolutionId = 0, uint gender = 0, uint costumeId = 0, bool shiny = false)
    {
        var evolutionSuffixes = (evolutionId > 0 ? new[] { "_e" + evolutionId, string.Empty } : new[] { string.Empty }).ToList();
        var formSuffixes = (formId > 0 ? new[] { "_f" + formId, string.Empty } : new[] { string.Empty }).ToList();
        var costumeSuffixes = (costumeId > 0 ? new[] { "_c" + costumeId, string.Empty } : new[] { string.Empty }).ToList();
        var genderSuffixes = (gender > 0 ? new[] { "_g" + (int)gender, string.Empty } : new[] { string.Empty }).ToList();
        var shinySuffixes = (shiny ? new[] { "_s", string.Empty } : new[] { string.Empty }).ToList();
        foreach (var evolutionSuffix in evolutionSuffixes)
        {
            foreach (var formSuffix in formSuffixes)
            {
                foreach (var costumeSuffix in costumeSuffixes)
                {
                    foreach (var genderSuffix in genderSuffixes)
                    {
                        foreach (var shinySuffix in shinySuffixes)
                        {
                            var result = $"{pokemonId}{evolutionSuffix}{formSuffix}{costumeSuffix}{genderSuffix}{shinySuffix}.{IconFormat}";
                            if (_availableForms.Contains(result))
                            {
                                return $"{_baseIconUrl}/{result}";
                            }
                        }
                    }
                }
            }
        }
        return GetDefaultIcon(_baseIconUrl); // Substitute Pokemon
    }

    #endregion

    #region Private Methods

    private async Task BuildIndexManifestAsync()
    {
        // Get the remote form index file from the icon repository
        var indexPath = Path.Combine(_baseIconUrl, IndexJson);
        var formsListJson = await NetUtils.GetAsync(indexPath);
        if (string.IsNullOrEmpty(formsListJson))
        {
            // Failed to get form list, skip...
            Console.WriteLine("Failed to download index.json or index was empty");
            return;
        }

        try
        {
            // Deserialize json list to hash set
            _availableForms = formsListJson?.FromJson<HashSet<string>>() ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse {IndexJson} for {indexPath}\nError: {ex.Message}");
        }
    }

    private string GetDefaultIcon(string baseUrl)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            return $"0.{IconFormat}";
        }
        return $"{baseUrl}/0.{IconFormat}";
    }

    #endregion
}