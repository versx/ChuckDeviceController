namespace ChuckDeviceController.Net.Models.Responses.Koji;

using System.Text.Json.Serialization;

/// <summary>
///     FeatureCollection and all geofences
/// </summary>
/// <example>
/// <code>
///     KojiApiResponse<List<KojiGeoJsonFeatureCollectionDataResponse<List<List<List<List<double>>>>>>>
/// </code>
/// </example>
public class KojiFeatureCollectionDataResponse : IBoundingBoxData, IGeoJsonFeatureType
{
    [JsonPropertyName("bbox")]
    public List<double>? BoundingBox { get; set; }

    [JsonPropertyName("features")]
    public List<GeoJsonFeature<List<List<MultiPolygon>>>>? Features { get; set; }

    [JsonPropertyName("type")]
    public FeatureType Type { get; set; }
}