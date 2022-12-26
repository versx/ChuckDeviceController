namespace ChuckDeviceController.Extensions.Json.Extensions;

using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

public static class PropertyExtensions
{
    public static void SetPropertyValue<T>(this PropertyInfo property, T instanceData, object? value)
    {
        try
        {
            var propertyDescriptor = property.GetPropertyDescriptor();
            if (propertyDescriptor == null)
                return;

            if (propertyDescriptor.PropertyType == typeof(object))
            {
                property.SetValue(instanceData, value);
            }
            else
            {
                var strValue = Convert.ToString(value);
                var converted = propertyDescriptor.Converter.ConvertFromString(strValue!);
                property.SetValue(instanceData, converted);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
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
        var properties = TypeDescriptor.GetProperties(propertyInfo.DeclaringType);
        var propertyDescriptor = properties.Find(propertyInfo.Name, ignoreCase: false);
        return propertyDescriptor;
    }
}