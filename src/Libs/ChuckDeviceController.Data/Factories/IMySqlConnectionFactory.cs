namespace ChuckDeviceController.Data.Factories;

using MySqlConnector;

public interface IMySqlConnectionFactory
{
    string ConnectionString { get; }

    MySqlConnection CreateConnection(bool open = true);

    Task<MySqlConnection> CreateConnectionAsync(bool open = true, CancellationToken stoppingToken = default);
}