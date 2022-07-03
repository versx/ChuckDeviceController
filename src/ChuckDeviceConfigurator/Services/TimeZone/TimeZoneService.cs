namespace ChuckDeviceConfigurator.Services.TimeZone
{
    using ChuckDeviceController.Extensions;

    public class TimeZoneService : ITimeZoneService
    {
        private const string TimeZonesFileName = "timezones.json";

        // TODO: Singleton pattern

        public IReadOnlyDictionary<string, TimeZoneOffsetData> TimeZones { get; }

        public TimeZoneService()
        {
            var filePath = TimeZonesFileName;// Path.Combine(Strings.BasePath, "wwwroot/data/", TimeZonesFileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{TimeZonesFileName} does not exist!", filePath);
            }
            TimeZones = filePath.LoadFromFile<Dictionary<string, TimeZoneOffsetData>>();
        }
    }
}