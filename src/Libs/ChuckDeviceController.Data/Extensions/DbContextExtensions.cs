namespace ChuckDeviceController.Data.Extensions
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class DbContextExtensions
    {
        public static async Task MigrateDatabase<TDbContext>(this IServiceProvider serviceProvider)
            where TDbContext : DbContext
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<IServiceProvider>();

            try
            {
                var dbContext = services.GetRequiredService<TDbContext>();
                if (dbContext == null)
                {
                    logger.LogError($"Failed to retrieve the required DbContext service from the service provider to migrate database");
                    return;
                }

                // Migrate the provided database context
                await dbContext.Database.MigrateAsync();
                logger.LogInformation($"Successfully migrated database context: {typeof(TDbContext).Name}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred while migrating the database context: {typeof(TDbContext).Name}");
            }
        }
    }
}