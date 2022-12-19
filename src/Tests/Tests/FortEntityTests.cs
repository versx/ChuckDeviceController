namespace Tests;

using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

using NUnit.Framework;
using POGOProtos.Rpc;
using static POGOProtos.Rpc.GameplayWeatherProto.Types;
using Type = System.Type;

using ChuckDeviceController.Data.Entities;

	internal class FortEntityTests
{
    private static readonly IReadOnlyDictionary<Type, Type> _enumsToConvert = new Dictionary<Type, Type>
    {
        { typeof(WeatherCondition), typeof(int) },
        { typeof(Team), typeof(int) },
        // SeenType
    };

    [SetUp]
    public void Setup()
    {
    }

    [TestCase]
    public void TestPokestops()
    {
        var pokestop = new Pokestop();
        _ = GetPropertyValues(pokestop);
        Assert.Pass();
    }

    [TestCase]
    public void TestGyms()
    {
        var gym = new Gym();
        _ = GetPropertyValues(gym);
        Assert.Pass();
    }

    [TestCase]
    public void TestPokemon()
    {
        var pokemon = new Pokemon();
        _ = GetPropertyValues(pokemon);
        Assert.Pass();
    }

    private static IEnumerable<object> GetPropertyValues<TEntity>(TEntity entity, IEnumerable<string>? ignoredProperties = null)
    {
        var results = new List<object>();
        var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        var properties = entity!.GetType().GetProperties(flags);
        //var properties = TypeDescriptor.GetProperties(typeof(TEntity));
        //foreach (PropertyDescriptor prop in properties)
        foreach (var prop in properties)
        {
            // Ignore any properties that are not mapped to a database table via an attribute
            //if (prop.Attributes[typeof(ColumnAttribute)] == null ||
            //    prop.Attributes[typeof(NotMappedAttribute)] != null)
            if (prop.GetCustomAttribute<ColumnAttribute>() == null ||
                prop.GetCustomAttribute<NotMappedAttribute>() != null)
                continue;

            // Ignore any virtual/database generated properties marked via attribute
            var generatedAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
            if (generatedAttr != null && generatedAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed)
                continue;

            // Ignore any properties specified that match the entity
            if (ignoredProperties?.Contains(prop.Name) ?? false)
                continue;

            // Check if property type is an enum and check if it's an enum we want to convert to an integer
            var value = prop.GetValue(entity) ?? null;
            //TestContext.WriteLine($"Property: {prop.Name}, Value: {value}");
            Console.WriteLine($"Property: {prop.Name}, Value: {value}");
            if (prop.PropertyType.IsEnum && _enumsToConvert.ContainsKey(prop.PropertyType))
            {
                var enumValue = Convert.ToString(value) ?? string.Empty;
                //var convertedValue = Convert.ToInt32(prop.Converter.ConvertFromString(enumValue));
                //yield return convertedValue;
                //yield return enumValue;
                results.Add(enumValue);
            }
            else if (prop.PropertyType == typeof(string) && value != null)
            {
                var strValue = Convert.ToString(value) ?? string.Empty;
                if (strValue.Contains('"') || strValue.Contains('\''))
                {
                    strValue = $"`{strValue}`";
                }
                else
                {
                    strValue = $"'{strValue}'";
                }
                //yield return strValue;
                results.Add(strValue);
            }
            else
            {
                value ??= "NULL"; //DBNull.Value;
                //yield return value;
                results.Add(value);
            }
        }
        return results;
    }
}
