namespace ChuckDeviceConfigurator.Services.TimeZone;

/// <summary>
/// Time zone service to retrieve related time zone offsets including
/// Daylight Savings Time or UTC.
/// </summary>
public interface ITimeZoneService
{
    /// <summary>
    /// Gets a dictionary of available time zones and their offset information.
    /// </summary>
    IReadOnlyDictionary<string, TimeZoneOffsetData> TimeZones { get; }
}