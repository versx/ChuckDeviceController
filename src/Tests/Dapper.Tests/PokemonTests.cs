namespace Dapper.Tests
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data;
    using System.Reflection;

    using Dapper;
    using MySqlConnector;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Extensions.Json;

    internal class PokemonTests
    {
        private const string ConnectionString = ""; // TODO: Add connection string to environment vars

        private MySqlConnection _connection = new(ConnectionString);

        [SetUp]
        public void SetUp()
        {
            _connection = new MySqlConnection(ConnectionString);
            Task.Run(async () => await _connection.OpenAsync()).Wait();
        }

        [TestCase("10041907652827335900")]
        public async Task TestPokemon(string id)
        {
            var tableName = GetTableAttribute<Pokemon>();
            var keyName = GetKeyAttribute<Pokemon>();

            SetTypeMap<Pokemon>();
            SqlMapper.SetTypeMap(typeof(Pokemon), new CustomPropertyTypeMap(typeof(SeenType), (type, columnName) =>
            {
                var property = type.GetProperties().FirstOrDefault(prop =>
                    prop.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .Any(attr => attr.Name == columnName));
                return property;
            }));

            SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Dictionary<string, dynamic>>>());
            SqlMapper.AddTypeHandler(new JsonTypeHandler<Dictionary<string, dynamic>>());
            SqlMapper.AddTypeHandler(new SeenTypeTypeHandler());


            using var connection = new MySqlConnection(ConnectionString);
            var sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{id}'";
            var entity = await connection.QueryFirstOrDefaultAsync<Pokemon>(sql, commandTimeout: 30);//, commandType: CommandType.Text);
            Assert.That(entity, Is.Null);
        }

        #region Attribute Helpers

        public static string? GetTableAttribute<TEntity>()
        {
            var attr = typeof(TEntity).GetCustomAttribute<TableAttribute>();
            var table = attr?.Name;
            return table;
        }

        public static string? GetKeyAttribute<TEntity>()
        {
            var type = typeof(TEntity);
            var properties = type.GetProperties();
            var attr = properties.FirstOrDefault(prop => prop.GetCustomAttribute<KeyAttribute>() != null);
            var key = attr?.Name;
            return key;
        }

        #endregion

        private static void SetTypeMap<TEntity>()
        {
            SqlMapper.SetTypeMap(
                typeof(TEntity),
                new CustomPropertyTypeMap(
                    typeof(TEntity),
                    (type, columnName) =>
                        type.GetProperties().FirstOrDefault(prop =>
                            prop.GetCustomAttributes(false)
                                .OfType<ColumnAttribute>()
                                .Any(attr => attr.Name == columnName))));
        }
    }

    public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
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

    public class SeenTypeTypeHandler : SqlMapper.TypeHandler<SeenType>
    {
        public override SeenType Parse(object value)
        {
            return Pokemon.StringToSeenType(value?.ToString() ?? string.Empty);
        }

        public override void SetValue(IDbDataParameter parameter, SeenType value)
        {
            var val = "'" + Pokemon.SeenTypeToString(value) + "'";
            parameter.Value = val;
        }
    }
}
