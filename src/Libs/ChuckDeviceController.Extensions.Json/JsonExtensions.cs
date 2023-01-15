namespace ChuckDeviceController.Extensions.Json;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = GetOptions(prettyPrint: false, converters: null);
    private static JsonSerializerOptions _jsonConverterOptions = GetOptions(prettyPrint: false, converters: null);

    public static T? FromJson<T>(this string json, bool prettyPrint = false, IEnumerable<JsonConverter>? converters = null)
    {
        try
        {
            if (converters != null)
            {
                if (_jsonConverterOptions.Converters.Count < converters.Count())
                {
                    _jsonConverterOptions = GetOptions(prettyPrint, converters: converters);
                }
            }

            var options = converters != null
                ? _jsonConverterOptions
                : _jsonOptions;
            var obj = JsonSerializer.Deserialize<T>(json, options);
            return obj;
        }
        catch //(Exception ex)
        {
            return default;
        }
    }

    public static string? ToJson<T>(this T obj, bool prettyPrint = false, IEnumerable<JsonConverter>? converters = null)
    {
        try
        {
            if (converters != null)
            {
                if (_jsonConverterOptions.Converters.Count < converters.Count())
                {
                    _jsonConverterOptions = GetOptions(prettyPrint, converters: converters);
                }
            }

            var options = converters != null
                ? _jsonConverterOptions
                : _jsonOptions;
            var json = JsonSerializer.Serialize(obj, options);
            return json;
        }
        catch //(Exception ex)
        {
            return default;
        }
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

    public static void SetConverters(IEnumerable<JsonConverter>? converters = null)
    {
        if (converters == null)
        {
            return;
        }

        _jsonConverterOptions = GetOptions(prettyPrint: true, converters: converters);
    }

    private static JsonSerializerOptions GetOptions(bool prettyPrint = false, IEnumerable<JsonConverter>? converters = null)
    {
        var options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = prettyPrint,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            //IgnoreReadOnlyProperties = true,
            //PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //ReferenceHandler = ReferenceHandler.Preserve,
        };
        if (converters != null)
        {
            foreach (var converter in converters)
            {
                if (options.Converters.Contains(converter))
                    continue;

                options.Converters.Add(converter);
            }
        }
        return options;
    }
}