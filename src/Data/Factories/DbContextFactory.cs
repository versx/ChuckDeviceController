namespace ChuckDeviceController.Data.Factories
{
    using System;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;

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
                ConsoleExt.WriteError($"[RawSql] Result: {ex}");
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