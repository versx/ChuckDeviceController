namespace ChuckDeviceController.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CircleInstanceRouteType : byte
    {
        /// <summary>
        /// Default leap frog routing logic.
        /// </summary>
        Default = 0,

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

        /// <summary>
        /// Dynamic route generation
        /// </summary>
        Dynamic,
    }
}