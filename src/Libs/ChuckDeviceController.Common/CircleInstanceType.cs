namespace ChuckDeviceController.Common;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CircleInstanceType
{
    Pokemon,
    Raid,
}