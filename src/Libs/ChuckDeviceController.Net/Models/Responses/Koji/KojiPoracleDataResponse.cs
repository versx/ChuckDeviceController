namespace ChuckDeviceController.Net.Models.Responses.Koji;

using System.Text.Json.Serialization;

/// <summary>
///     Poracle
/// </summary>
/// <example>
/// <code>
///     KojiApiResponse<List<KojiPoracleDataResponse<List<List<List<double>>>>>>
/// </code>
/// </example>
public class KojiPoracleDataResponse
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayInMinutes")]
    public bool DisplayInMatches { get; set; }

    [JsonPropertyName("userSelectable")]
    public bool UserSelectable { get; set; }

    [JsonPropertyName("multipath")]
    public List<MultiPolygon>? MultiPath { get; set; }
}