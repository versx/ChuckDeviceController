namespace ChuckDeviceController.Common.Data
{
    using System.Text.Json.Serialization;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum QuestMode
    {
        Normal,
        Alternative,
        Both,
    }
}