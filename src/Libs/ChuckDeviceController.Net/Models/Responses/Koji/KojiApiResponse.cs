namespace ChuckDeviceController.Net.Models.Responses.Koji;

using System.Net;
using System.Text.Json.Serialization;

public class KojiApiResponse<TDataResponse>
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("status_code")]
    public HttpStatusCode StatusCode { get; set; }

    [JsonPropertyName("data")]
    public TDataResponse? Data { get; set; }

    [JsonPropertyName("stats")]
    public KojiClusterStats? Stats { get; set; }
}

public class GeoJsonFeature<TCoordinates> : IGeoJsonFeature<TCoordinates>, IBoundingBoxData
{
    [JsonPropertyName("bbox")]
    public List<double>? BoundingBox { get; set; }

    [JsonPropertyName("geometry")]
    public GeoJsonFeatureGeometry<TCoordinates>? Geometry { get; set; }

    [JsonPropertyName("properties")]
    public GeoJsonFeatureProperties? Properties { get; set; }

    [JsonPropertyName("type")]
    public FeatureType Type { get; set; }
}

public class GeoJsonFeatureProperties
{
    [JsonPropertyName("id")]
    public uint Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class GeoJsonFeatureGeometry<TCoordinates> : IBoundingBoxData, IGeoJsonFeatureType
{
    [JsonPropertyName("bbox")]
    public List<double>? BoundingBox { get; set; }

    [JsonPropertyName("type")]
    public FeatureType Type { get; set; }

    [JsonPropertyName("coordinates")]
    public TCoordinates? Coordinates { get; set; }
}

public interface IBoundingBoxData
{
    List<double>? BoundingBox { get; }
}

public interface IGeoJsonFeatureType
{
    FeatureType Type { get; }
}

public interface IGeoJsonFeature<TCoordinates> : IGeoJsonFeatureType
{
    GeoJsonFeatureGeometry<TCoordinates>? Geometry { get; }

    GeoJsonFeatureProperties? Properties { get; }
}

public class Polygon : List<double>
{
}

public class MultiPolygon : List<Polygon>
{
}