namespace Dapper.Tests;

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
        //Task.Run(_connection.OpenAsync).Wait();
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
        Assert.Pass();
    }
}
