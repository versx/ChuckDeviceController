namespace ChuckDeviceController.Extensions
{
    using System.Text.Json;

    public static class JsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //IgnoreReadOnlyProperties = true,
            //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static T FromJson<T>(this string json) =>
            JsonSerializer.Deserialize<T>(json, _jsonOptions);

        public static string ToJson<T>(this T obj, bool pretty = false)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                WriteIndented = pretty,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            var json = JsonSerializer.Serialize(obj, options);
            return json;
        }
    }
}