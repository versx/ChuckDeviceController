namespace ChuckDeviceController.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CircleRouteType : ushort
    {
        Default = 0,
        Split,
        Circular,
    }
}