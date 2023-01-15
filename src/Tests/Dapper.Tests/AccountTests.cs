namespace Dapper.Tests;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Extensions;

internal class AccountTests
{
    [SetUp]
    public void Setup()
    {
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
