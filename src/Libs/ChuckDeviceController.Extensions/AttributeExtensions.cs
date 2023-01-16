namespace ChuckDeviceController.Extensions;

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

    public static string? GetColumnAttribute(this PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<ColumnAttribute>();
        var name = attr?.Name;
        return name;
    }

    public static bool IsGeneratedColumn(this PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<DatabaseGeneratedAttribute>();
        var result = (attr?.DatabaseGeneratedOption ?? DatabaseGeneratedOption.None) == DatabaseGeneratedOption.Computed;
        return result;
    }

    public static bool IsPrimaryKey(this PropertyInfo property)
    {
        return property.GetCustomAttribute<KeyAttribute>() != null;
    }

    public static (PropertyInfo?, string?) GetPrimaryKey(this IEnumerable<PropertyInfo> properties)
    {
        var property = properties.FirstOrDefault(x => x.IsPrimaryKey());
        var name = property?.GetColumnAttribute();
        return (property, name);
    }
}