namespace ChuckDeviceController.Data.Extensions
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public static class DbContextOptionsBuilderExtensions
    {
        public static MySqlDbContextOptionsBuilder GetMySqlOptions(this MySqlDbContextOptionsBuilder options, string assemblyName, int maxRetries = 10, int retryIntervalS = 15)
        {
            // TODO: options.MigrationsHistoryTable("");
            options.CommandTimeout(120); // TODO: Make 'MySql.CommandTimeout' configurable
            options.EnableIndexOptimizedBooleanColumns(true);
            options.EnableRetryOnFailure(maxRetries, TimeSpan.FromSeconds(retryIntervalS), null);
            options.MigrationsAssembly(assemblyName);
            return options;
        }
    }
}