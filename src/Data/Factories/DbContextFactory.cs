namespace ChuckDeviceController.Data.Factories
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;

    internal static class DbContextFactory
    {
        public static DeviceControllerContext CreateDeviceControllerContext(string connectionString)// where T : DbContext
        {
            var optionsBuilder = new DbContextOptionsBuilder<DeviceControllerContext>();
            //optionsBuilder.UseMySQL(connectionString);
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            //context.ChangeTracker.AutoDetectChangesEnabled = false;
            return new DeviceControllerContext(optionsBuilder.Options);
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