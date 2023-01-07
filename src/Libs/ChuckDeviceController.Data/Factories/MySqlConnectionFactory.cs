namespace ChuckDeviceController.Data.Factories;

using Microsoft.Extensions.Configuration;
using MySqlConnector;

public class MySqlConnectionFactory : IMySqlConnectionFactory
{
    //private readonly IConfiguration _configuration;
    //private readonly string? _connectionString;

    public string ConnectionString { get; }

    public MySqlConnectionFactory(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        //_configuration = configuration;
        ConnectionString = configuration.GetConnectionString("DefaultConnection")!;
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