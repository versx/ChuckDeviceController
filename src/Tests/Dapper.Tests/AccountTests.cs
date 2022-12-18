namespace Dapper.Tests
{
    using System.ComponentModel.DataAnnotations.Schema;

    using MySqlConnector;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Extensions;

    internal class AccountTests
    {
        private const string ConnectionString = "";

        private MySqlConnection _connection;

        [SetUp]
        public void Setup()
        {
            _connection = new MySqlConnection(ConnectionString);
            //Task.Run(async () => await _connection.OpenAsync()).Wait();
        }

        [Test]
        public void TestAccount()
        {
            var instance = Activator.CreateInstance(typeof(Account)) as Account;
            var columnNames = instance.GetPropertyNames();
            foreach (var columnName in columnNames)
            {
                Console.WriteLine($"Column: {columnName}");
            }
            Assert.IsTrue(true);
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
