namespace ChuckDeviceController.Extensions.Json.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;

using Extensions;

// Reference: https://josef.codes/custom-dictionary-string-object-jsonconverter-for-system-text-json/
public class ObjectDataConverter<T> : JsonConverter<T>
    where T : Dictionary<string, object?>
{
    //private static readonly IEnumerable<string> IgnoredProperties = new[]
    //{
    //    "Comparer",
    //    "Count",
    //    "Keys",
    //    "Values",
    //    "Item",
    //};

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
        }

        var result = Activator.CreateInstance<T>(); //new T();
        var properties = typeof(T)
            .GetProperties()
            //.Where(p => !IgnoredProperties.Contains(p.Name))
            .ToList();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            var propertyName = reader.GetString();
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }

            reader.Read();

            var value = ExtractValue(ref reader, options);
            result.Add(propertyName!, value);

            var property = properties.FirstOrDefault(prop => prop.GetJsonPropertyAttr()?.Name == propertyName);
            if (property == null)
                continue;

            property.SetPropertyValue(result, value);
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // We don't need any custom serialization logic for writing the json.
        // Ideally, this method should not be called at all. It's only called if you
        // supply JsonSerializerOptions that contains this JsonConverter in it's Converters list.
        // Don't do that, you will lose performance because of the cast needed below.
        // Cast to avoid infinite loop: https://github.com/dotnet/docs/issues/19268
        JsonSerializer.Serialize(writer, value, options);
    }

    private object? ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (reader.TryGetDateTime(out var date))
                {
                    return date;
                }
                return reader.GetString();
            case JsonTokenType.False:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var result))
                {
                    return result;
                }
                return reader.GetDecimal();
            case JsonTokenType.StartObject:
                return Read(ref reader, null!, options);
            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ExtractValue(ref reader, options));
                }
                return list;
            default:
                throw new JsonException($"'{reader.TokenType}' is not supported");
        }
    }
}