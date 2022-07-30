namespace ChuckDeviceController.Extensions.Json
{
    using System.IO;
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

        public static T? FromJson<T>(this string json) =>
            JsonSerializer.Deserialize<T>(json, _jsonOptions);

        public static string ToJson<T>(this T obj, bool pretty = false)
        {
            _jsonOptions.WriteIndented = pretty;
            var json = JsonSerializer.Serialize(obj, _jsonOptions);
            return json;
        }

        public static T? LoadFromFile<T>(this string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                Console.WriteLine($"{filePath} file is empty.");
                return default;
            }

            return data.FromJson<T>();
        }
    }
}