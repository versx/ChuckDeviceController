namespace ChuckDeviceController.Extensions.Data
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public static class DbContextOptionsBuilderExtensions
    {
        private const int MaxDatabaseRetry = 10; // TODO: Make 'MaxDatabaseRetry' configurable
        private const int DatabaseRetryIntervalS = 15; // TODO: Make 'DatabaseRetryIntervalS' configurable
        private const int DatabaseCommandTimeoutS = 30;//120; // TODO: Make 'DatabaseCommandTimeout' configurable

        public static MySqlDbContextOptionsBuilder GetMySqlOptions(this MySqlDbContextOptionsBuilder options, string assemblyName, int maxRetries = MaxDatabaseRetry, int retryIntervalS = DatabaseRetryIntervalS)
        {
            // TODO: options.MigrationsHistoryTable("");
            options.CommandTimeout(DatabaseCommandTimeoutS);
            options.EnableIndexOptimizedBooleanColumns(true);
            options.EnableRetryOnFailure(maxRetries, TimeSpan.FromSeconds(retryIntervalS), null);
            options.MigrationsAssembly(assemblyName);
            return options;
        }
    }
}