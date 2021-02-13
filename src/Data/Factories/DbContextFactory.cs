namespace ChuckDeviceController.Data.Factories
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;

    class DbContextFactory
    {
        public static DeviceControllerContext CreateDeviceControllerContext(string connectionString)// where T : DbContext
        {
            var optionsBuilder = new DbContextOptionsBuilder<DeviceControllerContext>();
            //optionsBuilder.UseMySQL(connectionString);
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            var context = new DeviceControllerContext(optionsBuilder.Options);
            //context.ChangeTracker.AutoDetectChangesEnabled = false;
            return context;
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