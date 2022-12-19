namespace ChuckDeviceConfigurator.Services.TimeZone;

using System.Text.Json.Serialization;

public class TimeZoneOffsetData
{
    [JsonPropertyName("utc")]
    public short Utc { get; set; }

    [JsonPropertyName("dst")]
    public short Dst { get; set; }
}