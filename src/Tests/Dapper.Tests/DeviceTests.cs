namespace Dapper.Tests
{
    using System.ComponentModel.DataAnnotations.Schema;

    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Repositories;

    internal class DeviceTests
    {
        private const string ConnectionString = ""; // TODO: Add connection string to environment vars

        private MySqlConnection _connection;

        [SetUp]
        public void Setup()
        {
            _connection = new MySqlConnection(ConnectionString);
            Task.Run(async () => await _connection.OpenAsync()).Wait();
        }

        [TestCase("atv08")]
        public async Task TestDevice(string uuid)
        {
            SetTypeMap<Device>();
            var device = await EntityRepository.GetEntityAsync<string, Device>(_connection, uuid, null, skipCache: true, setCache: false);
            Assert.That(device, Is.Not.Null);
        }

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
}