namespace Dapper.Tests
{
    using System.Data;

    using Dapper;
    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Data.Repositories;

    internal class IncidentTests
    {
        private const string ConnectionString = ""; // TODO: Add connection string to environment vars

        private MySqlConnection _connection = new(ConnectionString);

        [SetUp]
        public void SetUp()
        {
            _connection = new MySqlConnection(ConnectionString);
            Task.Run(async () => await _connection.OpenAsync()).Wait();
        }

        [TestCase("-1118716883437762032")]
        public async Task TestIncident(string id)
        {
            var tableName = typeof(Incident).GetTableAttribute();
            var keyName = typeof(Incident).GetKeyAttribute();

            EntityDataRepository.SetTypeMap<Incident>();

            using var connection = new MySqlConnection(ConnectionString);
            var sql = $"SELECT * FROM {tableName} WHERE {keyName} = '{id}'";
            var entity = await connection.QueryFirstOrDefaultAsync<Incident>(sql, commandTimeout: 30, commandType: CommandType.Text);
            Assert.That(entity, Is.Not.Null);
        }
    }
}
