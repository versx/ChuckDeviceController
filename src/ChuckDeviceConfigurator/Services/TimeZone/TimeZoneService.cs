namespace ChuckDeviceConfigurator.Services.TimeZone
{
    using ChuckDeviceController.Extensions.Json;

    public class TimeZoneService : ITimeZoneService
    {
        private const string TimeZonesFileName = "timezones.json";

        #region Singleton

        private static TimeZoneService? _instance;
        public static ITimeZoneService Instance => _instance ??= new TimeZoneService();

        #endregion

        public IReadOnlyDictionary<string, TimeZoneOffsetData> TimeZones { get; }

        public TimeZoneService()
        {
            var filePath = Path.Combine(Strings.DataFolder, TimeZonesFileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Time zone database file '{TimeZonesFileName}' does not exist!", filePath);
            }
            var obj = filePath.LoadFromFile<Dictionary<string, TimeZoneOffsetData>>();
            if (obj == null)
            {
                throw new NullReferenceException($"Failed to deserialize time zone manifest.");
            }
            TimeZones = obj;
        }
    }
}