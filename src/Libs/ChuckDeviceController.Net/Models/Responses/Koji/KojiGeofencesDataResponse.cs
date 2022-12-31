namespace ChuckDeviceController.Net.Models.Responses.Koji;

using System.Text.Json.Serialization;

public class KojiGeofencesDataResponse : IBoundingBoxData, IGeoJsonFeatureType
{
    [JsonPropertyName("bbox")]
    public List<double>? BoundingBox { get; set; }

    // Geometry type can be List<List<MultiPolygon>> or MultiPolygon
    [JsonPropertyName("features")]
    public List<GeoJsonFeature<dynamic>>? Features { get; set; }
    //public List<GeoJsonFeature<List<List<MultiPolygon>>>>? Features { get; set; }
    //public List<GeoJsonFeature<MultiPolygon>>? Features { get; set; }

    [JsonPropertyName("type")]
    public FeatureType Type { get; set; }
}