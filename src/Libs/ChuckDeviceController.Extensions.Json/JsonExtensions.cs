namespace ChuckDeviceController.Extensions.Json;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        //ReferenceHandler = ReferenceHandler.Preserve,
        //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //IgnoreReadOnlyProperties = true,
        //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static T? FromJson<T>(this string json) =>
        JsonSerializer.Deserialize<T>(json, _jsonOptions);

    public static T? FromJson<T>(this string json, IEnumerable<JsonConverter>? converters = null)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            //ReferenceHandler = ReferenceHandler.Preserve,
            //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //IgnoreReadOnlyProperties = true,
            //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        if (converters != null)
        {
            foreach (var converter in converters)
            {
                jsonOptions.Converters.Add(converter);
            }
        }
        var obj = JsonSerializer.Deserialize<T>(json, jsonOptions);
        return obj;
    }

    public static string ToJson<T>(this T obj, bool pretty = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            WriteIndented = pretty,
            ReadCommentHandling = JsonCommentHandling.Skip,
            //ReferenceHandler = ReferenceHandler.Preserve,
        };
        var json = JsonSerializer.Serialize(obj, options);
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