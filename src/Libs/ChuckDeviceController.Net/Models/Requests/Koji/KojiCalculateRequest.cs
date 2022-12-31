namespace ChuckDeviceController.Net.Models.Requests.Koji;

using System.Text.Json.Serialization;

public class KojiCalculateRequest : BaseKojiRequest
{
    // area / instance / data_points required
    [JsonPropertyName("data_points")]
    public object? DataPoints { get; set; } // TODO: List?

    [JsonPropertyName("radius")]
    public double? Radius { get; set; }

    [JsonPropertyName("min_points")]
    public uint? MinimumPoints { get; set; }

    [JsonPropertyName("generations")]
    public uint? Generations { get; set; }

    [JsonPropertyName("devices")]
    public List<string>? Devices { get; set; }

    [JsonPropertyName("fast")]
    public bool? Fast { get; set; }

    [JsonPropertyName("only_unique")]
    public bool? OnlyUnique { get; set; }
}