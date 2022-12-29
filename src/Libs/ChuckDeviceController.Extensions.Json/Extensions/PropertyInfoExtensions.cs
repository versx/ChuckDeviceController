namespace ChuckDeviceController.Extensions.Json.Extensions;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

public static class PropertyInfoExtensions
{
    //private static readonly IEnumerable<string> _ignoreProperties = new[]
    //{
    //    "Area",
    //    "Data",
    //};

    public static void SetPropertyValue<T>(this PropertyInfo property, T instance, object? value)
    {
        try
        {
            var propertyDescriptor = property.GetPropertyDescriptor();
            if (propertyDescriptor == null)
                return;

            if (propertyDescriptor.PropertyType == typeof(object))
            {
                // Data / Area
                //if (value?.GetType() == typeof(string))
                //if (property.Name == "Area")
                //{
                //    // Don't double serialize JSON string
                //    property.SetValue(instance, value.ToJson());
                //    return;
                //}

                property.SetValue(instance, value.ToJson());
            }
            else
            {
                var strValue = Convert.ToString(value);
                var converted = propertyDescriptor.Converter.ConvertFromString(strValue!);
                property.SetValue(instance, converted);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex}");
        }
    }

    public static JsonPropertyNameAttribute? GetJsonPropertyAttr(this PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attr;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    public static PropertyDescriptor GetPropertyDescriptor(this PropertyInfo propertyInfo)
    {
        var properties = TypeDescriptor.GetProperties(propertyInfo.DeclaringType!);
        var propertyDescriptor = properties.Find(propertyInfo.Name, ignoreCase: false);
        return propertyDescriptor!;
    }
}