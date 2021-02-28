namespace Chuck.Infrastructure.Data.Factories
{
    using System;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    using Chuck.Infrastructure.Data.Contexts;
    using Chuck.Infrastructure.Extensions;

    public static class DbContextFactory
    {
        public static DeviceControllerContext CreateDeviceControllerContext(string connectionString)// where T : DbContext
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<DeviceControllerContext>();
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var ctx = new DeviceControllerContext(optionsBuilder.Options);
                ctx.ChangeTracker.AutoDetectChangesEnabled = false;
                return ctx;
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