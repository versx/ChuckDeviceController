namespace Chuck.Infrastructure.Data.Contexts
{
    using System;
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;
    using POGOProtos.Rpc;

    using Chuck.Infrastructure.Data.Entities;
    using Chuck.Infrastructure.Data.Factories;

    public class DeviceControllerContext : DbContext
    {
        public DeviceControllerContext(DbContextOptions<DeviceControllerContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<Device> Devices { get; set; }

        public DbSet<DeviceGroup> DeviceGroups { get; set; }

        public DbSet<Instance> Instances { get; set; }

        public DbSet<Geofence> Geofences { get; set; }

        // Map entities
        public DbSet<Gym> Gyms { get; set; }

        public DbSet<GymDefender> GymDefenders { get; set; }

        public DbSet<Trainer> Trainers { get; set; }

        public DbSet<Pokemon> Pokemon { get; set; }

        public DbSet<Pokestop> Pokestops { get; set; }

        public DbSet<Cell> Cells { get; set; }

        public DbSet<Spawnpoint> Spawnpoints { get; set; }

        public DbSet<Weather> Weather { get; set; }

        public DbSet<Webhook> Webhooks { get; set; }

        public DbSet<IVList> IVLists { get; set; }

        public DbSet<Metadata> Metadata { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Instance>()
                        .Property(p => p.Type)
                        .HasConversion(x => Instance.InstanceTypeToString(x), x => Instance.StringToInstanceType(x));
            modelBuilder.Entity<Instance>()
                        .Property(nameof(Instance.Data))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<InstanceData>());
            modelBuilder.Entity<Geofence>()
                        .Property(p => p.Type)
                        .HasConversion(x => Geofence.GeofenceTypeToString(x), x => Geofence.StringToGeofenceType(x));
            modelBuilder.Entity<Geofence>()
                        .Property(p => p.Data)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<GeofenceData>());
            modelBuilder.Entity<Pokemon>()
                        .Property(p => p.PvpRankingsGreatLeague)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<PvpRank>>());
            modelBuilder.Entity<Pokemon>()
                        .Property(p => p.PvpRankingsUltraLeague)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<PvpRank>>());
            modelBuilder.Entity<Pokestop>()
                        .Property(nameof(Pokestop.QuestConditions))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<dynamic>>());
            modelBuilder.Entity<Pokestop>()
                        .Property(nameof(Pokestop.QuestRewards))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<dynamic>>());
            modelBuilder.Entity<Webhook>()
                        .Property(p => p.Types)
                        .HasConversion(x => Webhook.WebhookTypeToString(x), x => Webhook.StringToWebhookTypes(x));
            modelBuilder.Entity<Webhook>()
                        .Property(nameof(Webhook.Data))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<WebhookData>());
            modelBuilder.Entity<DeviceGroup>()
                        .Property(nameof(DeviceGroup.Devices))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<string>>());
            modelBuilder.Entity<IVList>()
                        .Property(nameof(IVList.PokemonIDs))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<ushort>>());

            base.OnModelCreating(modelBuilder);
        }
    }
}