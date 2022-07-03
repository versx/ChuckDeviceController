namespace ChuckDeviceConfigurator.Services.TimeZone
{
    public interface ITimeZoneService
    {
        IReadOnlyDictionary<string, TimeZoneOffsetData> TimeZones { get; }
    }
}