namespace ChuckDeviceController.Data.Factories
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;
    using System;

    internal static class DbContextFactory
    {
        public static DeviceControllerContext CreateDeviceControllerContext(string connectionString)// where T : DbContext
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<DeviceControllerContext>();
                //optionsBuilder.UseMySQL(connectionString);
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                //context.ChangeTracker.AutoDetectChangesEnabled = false;
                return new DeviceControllerContext(optionsBuilder.Options);
            }
            catch (Exception ex)
            {
                ConsoleColor org = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error RawSql Result: {ex.Message}");
                Console.ForegroundColor = org;
                return null;
            }
        }

        public static ValueConverter<T, string> CreateJsonValueConverter<T>()
        {
            return new ValueConverter<T, string>
            (
                v => v.ToJson(),
                v => v.FromJson<T>()
            );
        }
    }
}