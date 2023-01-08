namespace ChuckDeviceController.Data.Contexts;

using Microsoft.EntityFrameworkCore;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Factories;
using ChuckDeviceController.Extensions.Json.Converters;

public class ControllerDbContext : DbContext
{
    private static ulong _instanceCount;
    public static ulong InstanceCount => _instanceCount;

    #region DataSets

    public DbSet<Account> Accounts { get; set; } = null!;

    public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    public DbSet<Assignment> Assignments { get; set; } = null!;

    public DbSet<AssignmentGroup> AssignmentGroups { get; set; } = null!;

    public DbSet<Device> Devices { get; set; } = null!;

    public DbSet<DeviceGroup> DeviceGroups { get; set; } = null!;

    public DbSet<Geofence> Geofences { get; set; } = null!;

    public DbSet<Instance> Instances { get; set; } = null!;

    public DbSet<IvList> IvLists { get; set; } = null!;

    public DbSet<Webhook> Webhooks { get; set; } = null!;

    #endregion

    #region Constructor

    public ControllerDbContext(DbContextOptions<ControllerDbContext> options)
        : base(options)
    {
        Interlocked.Increment(ref _instanceCount);

        base.ChangeTracker.AutoDetectChangesEnabled = false;
        base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    #endregion

    #region Override Methods

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet("utf8mb4", DelegationModes.ApplyToAll);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(p => p.GroupName);
            entity.HasIndex(p => p.Level);
            entity.HasIndex(p => p.Failed);
            entity.HasIndex(p => p.FailedTimestamp);
            entity.HasIndex(p => p.FirstWarningTimestamp);
            entity.HasIndex(p => p.HasWarn);
            entity.HasIndex(p => p.WarnExpireTimestamp);
            entity.HasIndex(p => p.HasWarn);
            entity.HasIndex(p => p.WasSuspended);

            entity.Property(p => p.LastEncounterLatitude)
                  .HasPrecision(18, 6);
            entity.Property(p => p.LastEncounterLongitude)
                  .HasPrecision(18, 6);

            //entity.HasOne(a => a.Device)
            //      .WithOne(d => d.Account)
            //      .HasForeignKey(nameof(Account.Username))
            //      .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(p => p.ExpirationTimestamp);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasIndex(p => p.InstanceName);

            /*
            entity.HasOne(p => p.Instance)
                  .WithMany(p => p.Assignments)
                  .HasForeignKey(p => p.InstanceName);
            entity.HasOne(p => p.SourceInstance)
                  .WithMany(p => p.Assignment)
                  .HasForeignKey(p => p.SourceInstanceName);
            entity.HasOne(p => p.Device)
                  .WithMany(p => p.Assignments)
                  .HasForeignKey(p => p.DeviceUuid);
            entity.HasOne(p => p.DeviceGroup)
                  .WithMany(p => p.Assignments)
                  .HasForeignKey(p => p.DeviceGroupName);
            */
        });

        modelBuilder.Entity<AssignmentGroup>(entity =>
        {
            entity.Property(p => p.AssignmentIds)
                  .HasConversion(
                       DbContextFactory.CreateJsonValueConverter<List<uint>>(),
                       DbContextFactory.CreateValueComparer<uint>()
                   );
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasIndex(p => p.AccountUsername);
            entity.HasIndex(p => p.InstanceName);
            entity.HasIndex(p => p.LastSeen);

            entity.Property(p => p.LastLatitude)
                  .HasPrecision(18, 6);
            entity.Property(p => p.LastLongitude)
                  .HasPrecision(18, 6);

            entity.HasOne(d => d.Account);
                  //.WithOne(a => a.Username)
                  //.HasForeignKey(nameof(Device.AccountUsername));
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
                  .HasConversion(
                       x => Geofence.GeofenceTypeToString(x),
                       x => Geofence.StringToGeofenceType(x)
                   );
            entity.Property(p => p.Data)
                  .HasConversion(DbContextFactory.CreateJsonValueConverter<GeofenceData?>(new[] { new ObjectDataConverter<GeofenceData>() }));
        });

        modelBuilder.Entity<Instance>(entity =>
        {
            entity.Property(p => p.Type)
                  .HasConversion(
                       x => Instance.InstanceTypeToString(x),
                       x => Instance.StringToInstanceType(x)
                   );
            entity.Property(p => p.Data)
                  .HasConversion(DbContextFactory.CreateJsonValueConverter<InstanceData?>(new[] { new ObjectDataConverter<InstanceData>() }));
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
                  .HasConversion(
                       x => Plugin.PluginStateToString(x),
                       x => Plugin.StringToPluginState(x)
                   );
        });

        modelBuilder.Entity<Webhook>(entity =>
        {
            //entity.Property(p => p.Types)
            //      .HasConversion(
            //           x => Webhook.WebhookTypeToString(x),
            //           x => Webhook.StringToWebhookTypes(x),
            //           DbContextFactory.CreateValueComparer<WebhookType>()
            //       );
            entity.Property(p => p.Data)
                  .HasConversion(DbContextFactory.CreateJsonValueConverter<WebhookData?>());
                  //.HasConversion(DbContextFactory.CreateJsonValueConverter<WebhookData?>(new[] { new ObjectDataConverter<WebhookData>() }));
            entity.Property(nameof(Webhook.Geofences))
                  .HasConversion(
                       DbContextFactory.CreateJsonValueConverter<List<string>>(),
                       DbContextFactory.CreateValueComparer<string>()
                   );
        });

        base.OnModelCreating(modelBuilder);
    }

    #endregion
}