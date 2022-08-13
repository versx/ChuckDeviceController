namespace ChuckDeviceController.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;

    public class ControllerContext : DbContext
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ControllerContext(DbContextOptions<ControllerContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {
            //base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<AssignmentGroup> AssignmentGroups { get; set; }

        public DbSet<Device> Devices { get; set; }

        public DbSet<DeviceGroup> DeviceGroups { get; set; }

        public DbSet<Geofence> Geofences { get; set; }

        public DbSet<Instance> Instances { get; set; }

        public DbSet<IvList> IvLists { get; set; }

        public DbSet<Plugin> Plugins { get; set; }

        public DbSet<Webhook> Webhooks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssignmentGroup>()
                        .Property(p => p.AssignmentIds)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<uint>>());

            modelBuilder.Entity<DeviceGroup>()
                        .Property(p => p.DeviceUuids)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<string>>());

            modelBuilder.Entity<Geofence>()
                        .Property(p => p.Type)
                        .HasConversion(x => Geofence.GeofenceTypeToString(x), x => Geofence.StringToGeofenceType(x));
            modelBuilder.Entity<Geofence>()
                        .Property(nameof(Geofence.Data))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<GeofenceData>());

            modelBuilder.Entity<Instance>()
                        .Property(p => p.Type)
                        .HasConversion(x => Instance.InstanceTypeToString(x), x => Instance.StringToInstanceType(x));
            modelBuilder.Entity<Instance>()
                        .Property(nameof(Instance.Data))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<InstanceData>());
            modelBuilder.Entity<Instance>()
                        .Property(p => p.Geofences)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<string>>());

            modelBuilder.Entity<IvList>()
                        .Property(p => p.PokemonIds)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<string>>());

            modelBuilder.Entity<Plugin>()
                        .Property(p => p.State)
                        .HasConversion(x => Plugin.PluginStateToString(x), x => Plugin.StringToPluginState(x));

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