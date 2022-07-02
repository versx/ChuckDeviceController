namespace ChuckDeviceController.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CircleInstanceRouteType : byte
    {
        Default = 0,
        Split,
        Circular,
    }
}