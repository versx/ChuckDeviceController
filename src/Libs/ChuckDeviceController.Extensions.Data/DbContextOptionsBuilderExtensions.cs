namespace ChuckDeviceController.Extensions.Data
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public static class DbContextOptionsBuilderExtensions
    {
        public static MySqlDbContextOptionsBuilder GetMySqlOptions(
            this MySqlDbContextOptionsBuilder options,
            string assemblyName,
            MySqlResiliencyOptions resiliencyOptions)
        {
            options.CommandTimeout(resiliencyOptions.CommandTimeoutS);
            options.EnableIndexOptimizedBooleanColumns(true);
            options.EnableRetryOnFailure(resiliencyOptions.MaximumRetryCount, TimeSpan.FromSeconds(resiliencyOptions.RetryIntervalS), null);
            options.MigrationsAssembly(assemblyName);
            return options;
        }
    }
}