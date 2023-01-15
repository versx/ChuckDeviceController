namespace ChuckDeviceController.JobControllers;

using ChuckDeviceController.Common;

internal static class Strings
{
    public const ushort TenMinutesS = 600; // 10 minutes
    public const ushort ThirtyMinutesS = TenMinutesS * 3; // 30 minutes (1800)
    public const ushort SixtyMinutesS = ThirtyMinutesS * 2; // 60 minutes (3600)
    public const uint OneDayS = SixtyMinutesS * 24; // 24 hours (86400)

    public const string DefaultInstanceStatus = "--";
    public const ushort SpinRangeM = 80; // NOTE: Revert back to 40m once reverted ingame
    public const ulong DefaultDistance = 10000000000000000;
    public const ushort CooldownLimitS = SixtyMinutesS * 2; // 2 hours (7200)
    public const uint SuspensionTimeLimitS = OneDayS * 30; // 30 days (2592000) - Max account suspension time period

    #region Default Instance Property Values

    // All
    public const ushort DefaultMinimumLevel = 0;
    public const ushort DefaultMaximumLevel = 29;
    public const string DefaultAccountGroup = null;
    public const bool DefaultIsEvent = false;

    // Circle/Dynamic/Bootstrap
    public const CircleInstanceRouteType DefaultCircleRouteType = CircleInstanceRouteType.Smart;
    public const bool DefaultOptimizeDynamicRoute = true;

    // Smart Raid
    public const bool DefaultOptimizeSmartRaidRoute = true;
    public const ushort DefaultSmartRaidRadiusM = 500;

    // Quests
    public const short DefaultTimeZoneOffset = 0;
    public const string DefaultTimeZone = null;
    public const bool DefaultEnableDst = false;
    public const ushort DefaultSpinLimit = 3500;
    public const bool DefaultIgnoreS2CellBootstrap = false;
    public const bool DefaultUseWarningAccounts = false;
    public const QuestMode DefaultQuestMode = QuestMode.Normal;
    public const byte DefaultMaximumSpinAttempts = 5;
    public const ushort DefaultLogoutDelay = 900;

    // IV
    public const ushort DefaultIvQueueLimit = 100;
    public const string DefaultIvList = null;
    public const bool DefaultEnableLureEncounters = false;

    // Bootstrap
    public const bool DefaultFastBootstrapMode = false;
    public const ushort DefaultCircleSize = 70;
    public const bool DefaultOptimizeBootstrapRoute = true;
    public const string DefaultBootstrapCompleteInstanceName = null;

    // Tth Finder
    public const bool DefaultOnlyUnknownSpawnpoints = true;
    public const bool DefaultOptimizeSpawnpointRoute = true;

    // Leveling
    public const uint DefaultLevelingRadius = 10000;
    public const bool DefaultStoreLevelingData = false;
    public const string DefaultStartingCoordinate = null;

    // Custom
    public const string DefaultCustomInstanceType = null;

    #endregion
}