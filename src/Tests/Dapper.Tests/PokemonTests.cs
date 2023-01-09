namespace Dapper.Tests;

using System.Data;

using Dapper;
using MySqlConnector;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Data.TypeHandlers;
using ChuckDeviceController.Extensions;

internal class PokemonTests
{
    private const string ConnectionString = "";

    private MySqlConnection _connection = new(ConnectionString);

    [SetUp]
    public void SetUp()
    {
        _connection = new MySqlConnection(ConnectionString);
        Task.Run(_connection.OpenAsync).Wait();
    }

    [TestCase("10782810733712845072")] // encounter
    [TestCase("10792928927461492653")] // nearby_cell
    public async Task TestPokemon(string id)
    {
        var tableName = typeof(Pokemon).GetTableAttribute();
        var keyName = typeof(Pokemon).GetKeyAttribute();

        EntityDataRepository.SetTypeMap<Pokemon>();

        SqlMapper.AddTypeHandler(typeof(SeenType), SeenTypeTypeHandler.Default);
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Dictionary<string, dynamic>>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<Dictionary<string, dynamic>>());

        using var connection = new MySqlConnection(ConnectionString);
        var sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{id}'";
        var entity = await connection.QueryFirstOrDefaultAsync<Pokemon>(sql, commandTimeout: 30, commandType: CommandType.Text);
        Assert.That(entity, Is.Not.Null);
    }
}