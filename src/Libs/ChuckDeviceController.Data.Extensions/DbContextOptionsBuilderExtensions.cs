namespace ChuckDeviceController.Data.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public static class DbContextOptionsBuilderExtensions
    {
        public static MySqlDbContextOptionsBuilder GetMySqlOptions(this DbContextOptionsBuilder options, string assemblyName, int maxRetries = 10, int retryIntervalS = 15)
        {
            var mysqlOptions = new MySqlDbContextOptionsBuilder(options);
            // TODO: mysqlOptions.MigrationsHistoryTable("");
            mysqlOptions.CommandTimeout(120); // TODO: Make 'MySql.CommandTimeout' configurable
            mysqlOptions.EnableIndexOptimizedBooleanColumns(true);
            mysqlOptions.EnableRetryOnFailure(maxRetries, TimeSpan.FromSeconds(retryIntervalS), null);
            mysqlOptions.MigrationsAssembly(assemblyName);
            return mysqlOptions;
        }
    }
}