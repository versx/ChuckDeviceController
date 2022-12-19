namespace ChuckDeviceController.Data.Extensions;

using System.Data;

using MySqlConnector;

public static class MySqlConnectionExtensions
{
    private const uint DefaultConnectionWaitTimeS = 5;

    public static async Task WaitForConnectionAsync(
        this MySqlConnection connection,
        uint waitTimeS = DefaultConnectionWaitTimeS,
        CancellationToken stoppingToken = default)
    {
        var maxAttempts = 3;
        var attempts = 0;

        while ((connection?.State ?? ConnectionState.Closed) != ConnectionState.Open)
        {
            if (attempts >= maxAttempts)
                break;

            attempts++;
            await Task.Delay(TimeSpan.FromSeconds(waitTimeS), stoppingToken);
        }
    }
}