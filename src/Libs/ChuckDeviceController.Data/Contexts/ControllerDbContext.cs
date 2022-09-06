namespace ChuckDeviceController.Data.Contexts
{
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;

    public class ControllerDbContext : DbContext
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ControllerDbContext(DbContextOptions<ControllerDbContext> options)
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
            modelBuilder.Entity<Assignment>(entity =>
            {
                entity.HasIndex(p => p.InstanceName);
            });

            modelBuilder.Entity<AssignmentGroup>(entity =>
            {
                entity.Property(p => p.AssignmentIds)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<uint>>(),
                           DbContextFactory.CreateValueComparer<uint>()
                       );
            });

            modelBuilder.Entity<DeviceGroup>(entity =>
            {
                entity.Property(p => p.DeviceUuids)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<string>>(),
                           DbContextFactory.CreateValueComparer<string>()
                       );
            });

            modelBuilder.Entity<Geofence>(entity =>
            {
                entity.Property(p => p.Type)
                      .HasConversion(x => Geofence.GeofenceTypeToString(x), x => Geofence.StringToGeofenceType(x));
                entity.Property(p => p.Data)
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<GeofenceData?>());
            });

            modelBuilder.Entity<Instance>(entity =>
            {
                entity.Property(p => p.Type)
                      .HasConversion(x => Instance.InstanceTypeToString(x), x => Instance.StringToInstanceType(x));
                entity.Property(p => p.Data)
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<InstanceData?>());
                entity.Property(p => p.Geofences)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<string>>(),
                           DbContextFactory.CreateValueComparer<string>()
                       );
            });

            modelBuilder.Entity<IvList>(entity =>
            {
                entity.Property(p => p.PokemonIds)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<string>>(),
                           DbContextFactory.CreateValueComparer<string>()
                       );
            });

            modelBuilder.Entity<Plugin>(entity =>
            {
                entity.Property(p => p.State)
                      .HasConversion(x => Plugin.PluginStateToString(x), x => Plugin.StringToPluginState(x));
            });

            modelBuilder.Entity<Webhook>(entity =>
            {
                entity.Property(p => p.Types)
                      .HasConversion(x => Webhook.WebhookTypeToString(x), x => Webhook.StringToWebhookTypes(x),
                           DbContextFactory.CreateValueComparer<WebhookType>()
                       );
                entity.Property(p => p.Data)
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<WebhookData?>());
                entity.Property(nameof(Webhook.Geofences))
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<string>>(),
                           DbContextFactory.CreateValueComparer<string>()
                       );
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}