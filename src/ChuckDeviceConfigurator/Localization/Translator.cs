namespace ChuckDeviceConfigurator.Localization
{
    using Microsoft.Extensions.Logging;
    using ActivityType = POGOProtos.Rpc.HoloActivityType;
    using AlignmentType = POGOProtos.Rpc.PokemonDisplayProto.Types.Alignment;
    using CharacterCategory = POGOProtos.Rpc.EnumWrapper.Types.CharacterCategory;
    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;
    using ItemId = POGOProtos.Rpc.Item;
    using TemporaryEvolutionId = POGOProtos.Rpc.HoloTemporaryEvolutionId;
    using WeatherCondition = POGOProtos.Rpc.GameplayWeatherProto.Types.WeatherCondition;

    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Net.Utilities;

    public class Translator : Language<string, string, Dictionary<string, string>>
    {
        private static readonly ILogger<Translator> _logger =
            new Logger<Translator>(LoggerFactory.Create(x => x.AddConsole()));

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
                    return;
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
                Console.WriteLine($"Failed to find locale translation for key '{value}'");
                Console.WriteLine(ex);
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
                Console.WriteLine($"Failed to find locale translation for key '{value}' and arguments: '{string.Join(",", args)}'");
                Console.WriteLine(ex);
            }
            return value;
        }

        public string GetPokemonName(uint pokeId)
        {
            return Translate($"poke_{pokeId}");
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

        public string GetItem(ItemId item)
        {
            return Translate($"item_{(int)item}");
        }

        public string GetWeather(WeatherCondition weather)
        {
            return Translate($"weather_{(int)weather}");
        }

        public string GetAlignmentName(AlignmentType alignment)
        {
            return Translate($"alignment_{(int)alignment}");
        }

        public string GetCharacterCategoryName(CharacterCategory category)
        {
            return Translate($"character_category_{(int)category}");
        }

        public string GetEvolutionName(TemporaryEvolutionId evolution)
        {
            return Translate($"evo_{(int)evolution}");
        }

        public string GetGruntType(InvasionCharacter gruntType)
        {
            return Translate($"grunt_{(int)gruntType}");
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
            var files = Directory.GetFiles(_appLocalesFolder, extension)
                                 .Select(fileName => Path.GetFileName(fileName))
                                 .Where(fileName => fileName.StartsWith(prefix))
                                 .ToList();
            return files;
        }

        #endregion
    }
}