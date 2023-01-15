namespace ChuckDeviceController.Data.Factories;

using Microsoft.Extensions.Configuration;
using MySqlConnector;

public class MySqlConnectionFactory : IMySqlConnectionFactory
{
    public string ConnectionString { get; }

    public MySqlConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public MySqlConnectionFactory(IConfiguration configuration)
        : this(configuration.GetConnectionString("DefaultConnection")!)
    {
    }

    public MySqlConnection CreateConnection(bool open = true)
    {
        var connection = new MySqlConnection(ConnectionString);
        if (open)
        {
            connection.Open();
        }
        return connection;
    }

    public async Task<MySqlConnection> CreateConnectionAsync(bool open = true, CancellationToken stoppingToken = default)
    {
        var connection = new MySqlConnection(ConnectionString);
        if (open)
        {
            await connection.OpenAsync(stoppingToken);
        }
        return connection;
    }
}