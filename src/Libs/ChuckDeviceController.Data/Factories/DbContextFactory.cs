namespace ChuckDeviceController.Data.Factories
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions.Json;

    public static class DbContextFactory
    {
        public static T CreateDbContext<T>(string connectionString, string? assemblyName = null, bool autoDetectChanges = false) where T : DbContext
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<T>();
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), options =>
                {
                    if (!string.IsNullOrEmpty(assemblyName))
                    {
                        options.MigrationsAssembly(assemblyName);
                    }
                });

                var ctx = new DbContext(optionsBuilder.Options);
                ctx.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
                return (T)ctx;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RawSql] Result: {ex.Message}");
                Environment.Exit(0);
            }
            return null;
        }

        public static ControllerContext CreateControllerContext(string connectionString)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ControllerContext>();
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var ctx = new ControllerContext(optionsBuilder.Options);
                //ctx.ChangeTracker.AutoDetectChangesEnabled = false;
                return ctx;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RawSql] Result: {ex.Message}");
                Environment.Exit(0);
            }
            return null;
        }

        public static MapContext CreateMapDataContext(string connectionString)
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<MapContext>();
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                var ctx = new MapContext(optionsBuilder.Options);
                ctx.ChangeTracker.AutoDetectChangesEnabled = false;
                return ctx;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RawSql] Result: {ex.Message}");
                Environment.Exit(0);
            }
            return null;
        }


        public static DbContextOptionsBuilder BuildOptions<TContext>(string connectionString, string assemblyName)
            where TContext : DbContext
        {
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            return new DbContextOptionsBuilder()
                //.EnableSensitiveDataLogging()
                .UseMySql(connectionString, serverVersion, opt =>
                    opt.MigrationsAssembly(assemblyName)
                );
        }

        public static ValueConverter<T, string?> CreateJsonValueConverter<T>()
        {
            return new ValueConverter<T, string?>
            (
                v => v.ToJson(true),
                v => v!.FromJson<T>()!
            );
        }

        public static ValueComparer<List<T>> CreateValueComparer<T>()
        {
            return new ValueComparer<List<T>>
            (
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode())),
                c => c.ToList()
            );
        }

        public static ValueComparer<Dictionary<TKey, TValue>> CreateValueComparer<TKey, TValue>()
        {
            return new ValueComparer<Dictionary<TKey, TValue>>
            (
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v!.GetHashCode()))
            );
        }
    }
}