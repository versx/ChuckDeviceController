namespace ChuckDeviceController.Common;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CircleInstanceRouteType : byte
{
    /// <summary>
    /// Default leap frog routing logic.
    /// </summary>
    Default,

    /// <summary>
    /// Evenly split and spaced routing logic
    /// throughout route coordinates list.
    /// </summary>
    Split,

    //Circular,
    /// <summary>
    /// Smart circular spaced routing logic.
    /// </summary>
    Smart,
}