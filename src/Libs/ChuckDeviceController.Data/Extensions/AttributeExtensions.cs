namespace ChuckDeviceController.Data.Extensions;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

public static class AttributeExtensions
{
    public static string? GetTableAttribute(this Type type)
    {
        var attr = type.GetCustomAttribute<TableAttribute>();
        var table = attr?.Name;
        return table;
    }

    public static string? GetTableAttribute<TEntity>()
    {
        return GetTableAttribute(typeof(TEntity));
    }

    public static string? GetKeyAttribute(this Type type)
    {
        var properties = type.GetProperties();
        var attr = properties.FirstOrDefault(prop => prop.GetCustomAttribute<KeyAttribute>() != null);
        var key = attr?.Name;
        return key;
    }

    public static string? GetKeyAttribute<TEntity>()
    {
        return GetKeyAttribute(typeof(TEntity));
    }
}