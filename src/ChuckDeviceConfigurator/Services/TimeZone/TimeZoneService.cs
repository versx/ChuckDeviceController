namespace ChuckDeviceConfigurator.Services.TimeZone
{
    using ChuckDeviceController.Extensions;

    public class TimeZoneService : ITimeZoneService
    {
        private const string TimeZonesFileName = "timezones.json";

        #region Singleton

        private static TimeZoneService? _instance;
        public ITimeZoneService Instance => _instance ??= new TimeZoneService();

        #endregion

        public IReadOnlyDictionary<string, TimeZoneOffsetData> TimeZones { get; }

        public TimeZoneService()
        {
            var filePath = Path.Combine(Strings.DataFolder, TimeZonesFileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Time zone database file '{TimeZonesFileName}' does not exist!", filePath);
            }
            TimeZones = filePath.LoadFromFile<Dictionary<string, TimeZoneOffsetData>>();
        }
    }
}