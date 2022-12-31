namespace ChuckDeviceController.Net.Models.Responses.Koji;

using System.Text.Json.Serialization;

/// <summary>
///     Feature
/// </summary>
/// <example>
/// <code>
///     KojiApiResponse<List<KojiGeoJsonFeatureDataResponse<List<List<List<List<double>>>>>>>
/// </code>
/// </example>
public class KojiFeatureDataResponse : IGeoJsonFeature<List<List<MultiPolygon>>>, IBoundingBoxData
{
    [JsonPropertyName("bbox")]
    public List<double>? BoundingBox { get; set; }

    [JsonPropertyName("geometry")]
    public GeoJsonFeatureGeometry<List<List<MultiPolygon>>>? Geometry { get; set; }

    [JsonPropertyName("properties")]
    public GeoJsonFeatureProperties? Properties { get; set; }

    [JsonPropertyName("type")]
    public FeatureType Type { get; set; }
}