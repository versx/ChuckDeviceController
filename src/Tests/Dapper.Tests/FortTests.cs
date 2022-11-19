namespace Dapper.Tests
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Reflection;

    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions.Json;

    internal class FortTests
    {
        private const string ConnectionString = ""; // TODO: Add connection string to environment vars

        public const string GetPokestopById = "SELECT * FROM pokestop WHERE id=@id";

        private MySqlConnection _connection = new(ConnectionString);

        [SetUp]
        public void Setup()
        {
            _connection = new MySqlConnection(ConnectionString);
            Task.Run(async () => await _connection.OpenAsync()).Wait();
        }

        [TestCase("a59634f8aec34f9b8e7b265124d78ac8.16")]
        [TestCase("303642daef3d30a6bf491bf812d1d28b.16")]
        [TestCase("bb5005c44b4e4419b211d9eda729c84e.16")]
        public async Task TestPokestop(string pokestopId)
        {
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

            //var map = new CustomPropertyTypeMap(typeof(Pokestop), (type, columnName) =>
            //{
            //    var properties = type.GetProperties();
            //    var property = properties.FirstOrDefault(prop => GetColumnFromAttribute(prop) == columnName.ToLower());
            //    return property;
            //});
            //Dapper.SqlMapper.SetTypeMap(typeof(Pokestop), map);
            SetTypeMap<Pokestop>();
            Dapper.SqlMapper.AddTypeHandler(new CustomTypeHandler<List<Dictionary<string, dynamic>>>());

            //var pokestop = await _connection.QuerySingleOrDefaultAsync(sql, commandTimeout: 30);
            //var pokestop = await _connection.QueryFirstOrDefaultAsync<Pokestop>(GetPokestopById, new { id = pokestopId }, commandTimeout: 30);
            //Assert.That(pokestop, Is.Not.Null);

            var instance = (Pokestop)Activator.CreateInstance(typeof(Pokestop));
            var columnNames = GetPropertyNames(instance);//typeof(Pokestop));
            var columnNamesSql = string.Join(", ", columnNames);
            //var sql = $"SELECT {columnNamesSql} FROM pokestop WHERE id = '{pokestopId}'";
            var sql = $"SELECT * FROM pokestop WHERE id = '{pokestopId}'";
            var pokestop = await _connection.QueryFirstOrDefaultAsync<Pokestop>(sql);
            Assert.That(pokestop, Is.Not.Null);
        }

        private static void SetTypeMap<TEntity>()
        {
            Dapper.SqlMapper.SetTypeMap(
                typeof(TEntity),
                new CustomPropertyTypeMap(
                    typeof(TEntity),
                    (type, columnName) =>
                        type.GetProperties().FirstOrDefault(prop =>
                            prop.GetCustomAttributes(false)
                                .OfType<ColumnAttribute>()
                                .Any(attr => attr.Name == columnName))));
        }

        private static string GetColumnFromAttribute(MemberInfo member)
        {
            if (member == null) return null;

            var attrib = (ColumnAttribute)Attribute.GetCustomAttribute(member, typeof(ColumnAttribute), false);
            return (attrib?.Name ?? member.Name).ToLower();
        }

        public static IEnumerable<string> GetPropertyNames<TEntity>(
            TEntity entity,
            IEnumerable<string>? includedProperties = null,
            IEnumerable<string>? ignoredProperties = null)
        {
            // Include only public instance properties as well as base inherited properties
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            var properties = entity!.GetType().GetProperties(flags);
            //var properties = TypeDescriptor.GetProperties(typeof(TEntity));

            foreach (var prop in properties)
            {
                // Ignore any properties specified that match the entity
                if (ignoredProperties?.Contains(prop.Name) ?? false ||
                    !(includedProperties?.Contains(prop.Name) ?? true))
                    continue;

                // Ignore any properties that are not mapped to a database table via an attribute
                // or if the property is explicitly set to not be mapped.
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                if (columnAttr == null)// ||
                    //prop.GetCustomAttribute<NotMappedAttribute>() != null)
                    continue;

                // Ignore any virtual/database generated properties marked via attribute
                var generatedAttr = prop.GetCustomAttribute<DatabaseGeneratedAttribute>();
                if (generatedAttr != null && generatedAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Computed)
                    continue;

                yield return $"{columnAttr.Name} AS {prop.Name}";
                //yield return prop.Name;
            }
        }
    }

    public class CustomTypeHandler<T> : SqlMapper.TypeHandler<T>
    {
        public override T Parse(object value)
        {
            var json = value.ToString();
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }
            var obj = json.FromJson<T>();
            return obj ?? default;
        }

        public override void SetValue(IDbDataParameter parameter, T value)
        {
            var json = value?.ToJson();
            parameter.Value = json ?? null;
        }
    }
}
