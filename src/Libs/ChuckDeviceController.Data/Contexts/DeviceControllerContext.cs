namespace ChuckDeviceController.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;

    public class DeviceControllerContext : DbContext
    {
        public DeviceControllerContext(DbContextOptions<DeviceControllerContext> options)
            : base(options)
        {
            // Migrate to latest
            //var createSql = Database.GenerateCreateScript();
            //Console.WriteLine($"CreateSql: {createSql}");
            //base.Database.Migrate();
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Assignment> Assignments { get; set; }

        // TODO: AssignmentGroups

        public DbSet<Device> Devices { get; set; }

        public DbSet<DeviceGroup> DeviceGroups { get; set; }

        public DbSet<Geofence> Geofences { get; set; }

        public DbSet<Instance> Instances { get; set; }

        public DbSet<IvList> IvLists { get; set; }

        public DbSet<Webhook> Webhooks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceGroup>()
                        .Property(p => p.Devices)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<string>>());

            modelBuilder.Entity<Geofence>()
                        .Property(p => p.Type)
                        .HasConversion(x => Geofence.GeofenceTypeToString(x), x => Geofence.StringToGeofenceType(x));
            modelBuilder.Entity<Geofence>()
                        .Property(p => p.Data)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<GeofenceData>());

            modelBuilder.Entity<Instance>()
                        .Property(p => p.Type)
                        .HasConversion(x => Instance.InstanceTypeToString(x), x => Instance.StringToInstanceType(x));
            modelBuilder.Entity<Instance>()
                        .Property(p => p.Data)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<InstanceData>());
            modelBuilder.Entity<Instance>()
                        .Property(p => p.Geofences)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<string>>());

            modelBuilder.Entity<IvList>()
                        .Property(p => p.PokemonIds)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<uint>>());

            modelBuilder.Entity<Webhook>()
                        .Property(p => p.Types)
                        .HasConversion(x => Webhook.WebhookTypeToString(x), x => Webhook.StringToWebhookTypes(x));
            modelBuilder.Entity<Webhook>()
                        .Property(nameof(Webhook.Data))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<WebhookData>());
            modelBuilder.Entity<Webhook>()
                        .Property(nameof(Webhook.Geofences))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<string>>());

            base.OnModelCreating(modelBuilder);
        }
    }
}