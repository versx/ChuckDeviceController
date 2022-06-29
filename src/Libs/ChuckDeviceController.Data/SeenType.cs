namespace ChuckDeviceController.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SeenType
    {
        Unset,
        Encounter,
        Wild,
        NearbyStop,
        NearbyCell,
        LureWild,
        LureEncounter,
    }
}