namespace ChuckDeviceController.Extensions.Json.Converters;

using System.Text.Json;
using System.Text.Json.Serialization;

using Extensions;

// Reference: https://josef.codes/custom-dictionary-string-object-jsonconverter-for-system-text-json/
public class ObjectDataConverter<T> : JsonConverter<T>
    where T : Dictionary<string, object?>
{
    private static readonly IEnumerable<string> IgnoredProperties = new[]
    {
        "Comparer",
        "Count",
        "Keys",
        "Values",
        "Item",
    };

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
        }

        // Instantiate instance of generic return type
        var result = Activator.CreateInstance<T>();
        // Retrieve list of properties of generic instance type
        var properties = typeof(T)
            .GetProperties()
            .Where(p => !IgnoredProperties.Contains(p.Name))
            .ToList();

        // Keep reading until end of JSON stream
        while (reader.Read())
        {
            // If end of stream is end token (i.e. `}`), return results
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            // Ensure current token read is the property name
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            // Read property name from next token
            var propertyName = reader.GetString();
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }

            // Read next token in JSON stream aka the value of the property
            reader.Read();

            // Extract the value type from the stream
            var value = ExtractValue(ref reader, options);

            // Add property key and value to dictionary instance of generic type
            result.Add(propertyName!, value);

            // Match the explicit property name with the dictionary inherited
            // defined property decorated with the JsonPropertyNameAttribute.
            var property = properties.FirstOrDefault(prop => prop.GetJsonPropertyAttr()?.Name == propertyName);
            // If no match, i.e. property is probably 'Comparer, Count, Keys, Values, Item', then skip it.
            if (property == null)
                continue;

            // Set explicit property value
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

        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            //writer.WriteStartObject();
            //var obj = FlattenDictionary(value);
            //foreach (var (key, val) in obj)
            //{
            //    writer.WritePropertyName(key);
            //    JsonSerializer.Serialize(writer, val, options);
            //}
            //writer.WriteEndObject();

            // .NET's System.Text.Json.JsonSerializer class will only serialize
            // and or deserialize dictionary key/values for classes that inherit
            // from dictionaries. Any custom properties set in the custom class
            // will not be populated or used.
            // 
            // We work around this by creating a new dictionary and adding the
            // custom properties values set as well as including any key/value
            // pairs set in the base dictionary class.
            // 
            // Not the best solution, although it works. Issues can and will occur
            // if there is a key and property declared as the same name.

            // TODO: Potentially just use `JsonExtensionData` attribute, although
            // some refactoring will need to be done.
            var obj = FlattenDictionary(value);
            JsonSerializer.Serialize(writer, obj);
        }
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

    private static Dictionary<string, object?> FlattenDictionary(T data)
    {
        var result = new Dictionary<string, object?>();
        // Retrieve list of properties for generic instance type
        var properties = typeof(T)
            .GetProperties()
            .Where(prop => !IgnoredProperties.Contains(prop.Name))
            .ToList();

        // Loop through explicit properties of entity
        foreach (var prop in properties)
        {
            // Only check properties with the `JsonPropertyName`
            // attribute set.
            var jsonName = prop.GetJsonPropertyAttr()?.Name;
            if (jsonName == null)
                continue;

            // Retrieve the property value from the 
            var value = prop.GetValue(data);
            // Set the properties as their `JsonPropertyName` attribute
            // description since we are writing the object.
            result[jsonName] = value; // i.e. `area`
            //result[prop.Name] = value; // i.e. `Area`
        }

        // Loop through inherited key/value pair dictionary and set
        // the property name and values in the flat dictionary.
        foreach (var (key, value) in data)
        {
            result[key] = value;
        }

        return result;
    }
}