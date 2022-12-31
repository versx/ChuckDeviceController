namespace ChuckDeviceController.Net.Models;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureType
{
    Bounds,
    Text,
    SingleArray,
    MultiArray,
    SingleStruct,
    MultiStruct,
    Feature,
    FeatureVec,
    FeatureCollection,
    MultiPoint,
    Polygon,
    MultiPolygon,
}