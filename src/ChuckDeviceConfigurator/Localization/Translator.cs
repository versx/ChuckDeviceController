namespace ChuckDeviceConfigurator.Localization;

using System.Text;

using Microsoft.Extensions.Logging;
using POGOProtos.Rpc;
using ActivityType = POGOProtos.Rpc.HoloActivityType;
using AlignmentType = POGOProtos.Rpc.PokemonDisplayProto.Types.Alignment;
using CharacterCategory = POGOProtos.Rpc.EnumWrapper.Types.CharacterCategory;
using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
using ItemId = POGOProtos.Rpc.Item;
using TemporaryEvolutionId = POGOProtos.Rpc.HoloTemporaryEvolutionId;
using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

using ChuckDeviceController.Extensions;
using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Logging;
using ChuckDeviceController.Net.Utilities;
using ChuckDeviceController.Plugin;

public class Translator : Language<string, string, Dictionary<string, string>>, ILocalizationHost
{
    private static readonly ILogger<Translator> _logger =
        GenericLoggerFactory.CreateLogger<Translator>();

    private const string SourceLocaleUrl = "https://raw.githubusercontent.com/WatWowMap/pogo-translations/master/static/locales/";
    private static readonly string _appLocalesFolder = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{Strings.LocaleFolder}";
    private static readonly string _binLocalesFolder = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{Strings.BasePath}{Path.DirectorySeparatorChar}{Strings.LocaleFolder}";

    #region Singleton

    private static Translator? _instance;

    public static Translator Instance =>
        _instance ??= new Translator
        {
            LocaleDirectory = _binLocalesFolder,
            //CurrentCulture = 
        };

    #endregion

    #region Constructor

    public Translator()
    {
        if (!Directory.Exists(_appLocalesFolder))
        {
            throw new DirectoryNotFoundException($"Source locales directory does not exist '{_appLocalesFolder}', unable to generate locale files");
        }
    }

    #endregion

    #region Static Methods

    public static async Task CreateLocaleFilesAsync()
    {
        // Get a list of base locale file names
        var baseLocaleFileNames = GetBaseLocaleFileNames();

        // Copy any missing base locale files to bin directory
        await CopyLocaleFilesAsync(baseLocaleFileNames, ignoreExisting: true);

        foreach (var file in baseLocaleFileNames)
        {
            // Replace locale prefix
            var localeFile = Path.GetFileName(file).Replace("_", null);
            var locale = Path.GetFileNameWithoutExtension(localeFile);

            var url = SourceLocaleUrl + localeFile;
            var json = await NetUtils.GetAsync(url);
            if (json == null)
            {
                _logger.LogWarning($"Failed to fetch locales from {url}, skipping...");
                continue;
            }

            _logger.LogInformation($"Creating locale {locale}...");
            var remote = json.FromJson<Dictionary<string, string>>();
            if (remote == null)
            {
                _logger.LogError($"Failed to deserialize locale file '{localeFile}'");
                continue;
            }

            foreach (var (key, _) in remote)
            {
                // Make locale variables compliant with Handlebars/Mustache templating
                remote[key] = remote[key].Replace("%", "{")
                                         .Replace("}", "}}");
            }

            if (locale != "en")
            {
                // Include en as fallback first
                var enTransFallback = File.ReadAllText(
                    Path.Combine(_appLocalesFolder, "_en.json")
                );
                var fallbackTranslations = enTransFallback.FromJson<Dictionary<string, string>>();
                if (fallbackTranslations != null)
                {
                    remote = remote.Merge(fallbackTranslations, updateValues: true);
                }
            }

            var appTranslationsData = File.ReadAllText(Path.Combine(_appLocalesFolder, file));
            var appTranslations = appTranslationsData.FromJson<Dictionary<string, string>>();
            if (appTranslations != null)
            {
                remote = remote.Merge(appTranslations, updateValues: true);
            }

            File.WriteAllText(
                Path.Combine(_binLocalesFolder, localeFile),
                remote.ToJson(pretty: true)
            );
            _logger.LogInformation($"{locale} file saved.");

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    #endregion

    #region Public Methods

    public override string Translate(string value)
    {
        try
        {
            return base.Translate(value) ?? value;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to find locale translation for key '{value}': {ex}");
        }
        return value;
    }

    public string Translate(string value, params object[] args)
    {
        try
        {
            var text = args?.Length > 0
                ? string.Format(base.Translate(value), args)
                : base.Translate(value);
            return text ?? value;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to find locale translation for key '{value}' and arguments: '{string.Join(",", args)}': {ex}");
        }
        return value;
    }

    public string GetPokemonName(uint pokeId)
    {
        return Translate($"poke_{pokeId}");
    }

    public string GetPokemonName(string pokemonFormCostumeGenderId)
    {
        var split = pokemonFormCostumeGenderId.Split('_');
        if (!split.Any())
        {
            return pokemonFormCostumeGenderId;
        }

        var details = GetPokemonDetails(pokemonFormCostumeGenderId);            
        var pokemonName = GetPokemonName(details.PokemonId);

        var sb = new StringBuilder();
        sb.Append(pokemonName);

        if (details.FormId > 0)
        {
            var formName = GetFormName(details.FormId);
            sb.Append($" ({formName})");
        }
        if (details.CostumeId > 0)
        {
            var costumeName = GetCostumeName(details.CostumeId);
            sb.Append($" {costumeName}");
        }
        if (details.GenderId > 0)
        {
            var genderName = GetGenderName(details.GenderId);
            sb.Append($" {genderName}");
        }
        var name = sb.ToString();
        return name;
    }

    public string GetGenderName(ushort genderId)
    {
        return Translate($"gender_{genderId}");
    }

    public string GetFormName(uint formId, bool includeNormal = false)
    {
        if (formId == 0)
            return null;

        var form = Translate("form_" + formId);
        var normal = Translate("NORMAL");
        if (!includeNormal && string.Compare(form, normal, true) == 0)
            return string.Empty;
        return form;
    }

    public string GetCostumeName(uint costumeId)
    {
        if (costumeId == 0)
            return null;

        var costume = Translate("costume_" + costumeId);
        return costume;
    }

    public string GetEvolutionName(TemporaryEvolutionId evolution)
    {
        if (evolution == TemporaryEvolutionId.TempEvolutionUnset)
            return null;

        return Translate($"evo_{(int)evolution}");
    }

    public string GetEvolutionName(uint evoId)
    {
        if (evoId == 0)
            return null;

        var evo = Translate("evo_" + evoId);
        return evo;
    }

    public string GetMoveName(uint moveId)
    {
        if (moveId == 0)
            return Translate("UNKNOWN");

        return Translate($"move_{moveId}");
    }

    public string GetThrowName(ActivityType throwTypeId)
    {
        return Translate($"throw_type_{(int)throwTypeId}");
    }

    public string GetThrowName(uint throwTypeId)
    {
        return GetThrowName((ActivityType)throwTypeId);
    }

    public string GetItem(ItemId item)
    {
        return Translate($"item_{(int)item}");
    }

    public string GetItem(uint itemId)
    {
        return GetItem((ItemId)itemId);
    }

    public string GetWeather(WeatherCondition weather)
    {
        return Translate($"weather_{(int)weather}");
    }

    public string GetWeather(uint weatherConditionId)
    {
        return GetWeather((WeatherCondition)weatherConditionId);
    }

    public string GetAlignmentName(AlignmentType alignment)
    {
        return Translate($"alignment_{(int)alignment}");
    }

    public string GetAlignmentName(uint alignmentTypeId)
    {
        return GetAlignmentName((AlignmentType)alignmentTypeId);
    }

    public string GetCharacterCategoryName(CharacterCategory category)
    {
        return Translate($"character_category_{(int)category}");
    }

    public string GetCharacterCategoryName(uint characterCategoryId)
    {
        return GetCharacterCategoryName((CharacterCategory)characterCategoryId);
    }

    public string GetGruntType(InvasionCharacter gruntType)
    {
        return Translate($"grunt_{(int)gruntType}");
    }

    public string GetGruntType(uint invasionCharacterId)
    {
        return GetGruntType((InvasionCharacter)invasionCharacterId);
    }

    public string GetTeam(Team team)
    {
        return GetTeam((ushort)team);
    }

    public string GetTeam(ushort teamId)
    {
        return Translate($"team_{teamId}");
    }

    public PokemonDetails GetPokemonDetails(string pokemonFormCostumeGenderId)
    {
        var split = pokemonFormCostumeGenderId.Split('_');
        if (!split.Any())
        {
            return new();
        }

        var pokemonId = split[0];
        var formId = split.Length > 1 ? split.FirstOrDefault(item => item.Contains('f')) : null;
        var costumeId = split.Length > 1 ? split.FirstOrDefault(item => item.Contains('c')) : null;
        var genderId = split.Length > 1 ? split.FirstOrDefault(item => item.Contains('g')) : null;

        var details = new PokemonDetails
        {
            PokemonId = Convert.ToUInt32(pokemonId ?? "0"),
            FormId = Convert.ToUInt32(formId ?? "0"),
            CostumeId = Convert.ToUInt32(costumeId ?? "0"),
            GenderId = Convert.ToUInt16(genderId ?? "0"),
        };
        return details;
    }

    #endregion

    #region Private Methods

    private static async Task CopyLocaleFilesAsync(IReadOnlyList<string> baseLocaleFileNames, bool ignoreExisting = true)
    {
        // Copy base locale files from app directory to bin directory if they do not exist
        foreach (var baseLocaleFileName in baseLocaleFileNames)
        {
            // Replace locale prefix
            var localeFile = baseLocaleFileName;
            var localeBin = Path.Combine(_binLocalesFolder, localeFile);
            if (ignoreExisting && File.Exists(localeBin))
                continue;

            _logger.LogDebug($"Copying base locale '{localeFile}' to {localeBin}...");
            var baseLocalePath = Path.Combine(_appLocalesFolder, baseLocaleFileName);
            File.Copy(baseLocalePath, localeBin);
        }

        await Task.CompletedTask;
    }

    private static IReadOnlyList<string> GetBaseLocaleFileNames(string extension = "*.json", string prefix = "_")
    {
        // Get a list of locale file names that have prefix '_'
        var files = Directory
            .GetFiles(_appLocalesFolder, extension)
            .Select(fileName => Path.GetFileName(fileName))
            .Where(fileName => fileName.StartsWith(prefix))
            .ToList();
        return files;
    }

    #endregion
}

public class PokemonDetails
{
    public uint PokemonId { get; set; }

    public uint FormId { get; set; }

    public uint CostumeId { get; set; }

    public ushort GenderId { get; set; }
}