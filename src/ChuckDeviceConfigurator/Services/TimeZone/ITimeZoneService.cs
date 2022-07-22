namespace ChuckDeviceConfigurator.Services.TimeZone
{
    /// <summary>
    /// Time zone service to retrieve related time zone offsets including
    /// Daylight Savings Time or UTC.
    /// </summary>
    public interface ITimeZoneService
    {
        IReadOnlyDictionary<string, TimeZoneOffsetData> TimeZones { get; }
    }
}