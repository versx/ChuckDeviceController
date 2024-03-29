﻿namespace ChuckDeviceController.Extensions.Json;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = GetDefaultOptions(prettyPrint: false, converters: null);

    public static T? FromJson<T>(this string json)
    {
        try
        {
            var obj = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return obj;
        }
        catch //(Exception ex)
        {
            return default;
        }
    }

    public static T? FromJson<T>(this string json, IEnumerable<JsonConverter>? converters = null)
    {
        try
        {
            var options = GetDefaultOptions(prettyPrint: true, converters);
            var obj = JsonSerializer.Deserialize<T>(json, options);
            return obj;
        }
        catch //(Exception ex)
        {
            return default;
        }
    }

    public static string? ToJson<T>(this T obj, bool pretty = false, IEnumerable<JsonConverter>? converters = null)
    {
        try
        {
            var options = GetDefaultOptions(prettyPrint: pretty, converters);
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

    public static JsonSerializerOptions GetDefaultOptions(bool prettyPrint = false, IEnumerable<JsonConverter>? converters = null)
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
                options.Converters.Add(converter);
            }
        }
        return options;
    }
}