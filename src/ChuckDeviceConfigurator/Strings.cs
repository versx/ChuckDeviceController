using ChuckDeviceController.Data;

namespace ChuckDeviceConfigurator
{
    public static partial class Strings
    {
        public const string WebRoot = "wwwroot";

        public static readonly string DataFolder = Path.Combine(WebRoot, "data");

        public static readonly string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        public static readonly string AssemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public const string SuperAdminRole = "SuperAdmin";

        public const string DefaultUserName = "root";

        public const string DefaultUserPassword = "123Pa$$word.";

        public const string DefaultSuccessLoginPath = "/Identity/Account/Manage";

        public const string DefaultInstanceStatus = "--";

        public const ushort ThirtyMinutesS = 1800;
        public const ushort SixtyMinutesS = ThirtyMinutesS * 2;

        public const string PokemonImageUrl = "https://raw.githubusercontent.com/WatWowMap/wwm-uicons/main/pokemon/";
        public const string GoogleMapsLinkFormat = "https://maps.google.com/maps?q={0},{1}";


        // Default Instance Property Values
        public const ushort DefaultMinimumLevel = 0;
        public const ushort DefaultMaximumLevel = 29;

        public const CircleInstanceRouteType DefaultCircleRouteType = CircleInstanceRouteType.Default;
        public const bool DefaultOptimizeDynamicRoute = true;

        public const short DefaultTimeZoneOffset = 0;
        public const string DefaultTimeZone = null;
        public const bool DefaultEnableDst = false;
        public const ushort DefaultSpinLimit = 3500;
        public const bool DefaultIgnoreS2CellBootstrap = false;
        public const bool DefaultUseWarningAccounts = false;
        public const QuestMode DefaultQuestMode = QuestMode.Normal;
        public const byte DefaultMaximumSpinAttempts = 5;
        public const ushort DefaultLogoutDelay = 900;

        public const ushort DefaultIvQueueLimit = 100;
        public const string DefaultIvList = null;
        public const bool DefaultEnableLureEncounters = false;

        public const bool DefaultFastBootstrapMode = false;
        public const ushort DefaultCircleSize = 70;
        public const bool DefaultOptimizeBootstrapRoute = true;
        public const string DefaultBootstrapCompleteInstanceName = null;

        public const bool DefaultOnlyUnknownSpawnpoints = true;
        public const bool DefaultOptimizeSpawnpointRoute = true;

        public const string DefaultAccountGroup = null;
        public const bool DefaultIsEvent = false;
    }
}