namespace ChuckDeviceController.Data.Contexts
{
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Data.Triggers;

    public class MapDbContext : DbContext
    {
        public static ulong InstanceCount;

        #region Properties

        // Map entities
        public DbSet<Gym> Gyms { get; set; } = null!;

        public DbSet<GymDefender> GymDefenders { get; set; } = null!;

        public DbSet<GymTrainer> GymTrainers { get; set; } = null!;

        public DbSet<Pokemon> Pokemon { get; set; } = null!;

        public DbSet<Pokestop> Pokestops { get; set; } = null!;

        public DbSet<Incident> Incidents { get; set; } = null!;

        public DbSet<Cell> Cells { get; set; } = null!;

        public DbSet<Spawnpoint> Spawnpoints { get; set; } = null!;

        public DbSet<Weather> Weather { get; set; } = null!;

        #region Pokemon Statistics

        public DbSet<PokemonStats> PokemonStats { get; set; } = null!;

        public DbSet<PokemonIvStats> PokemonIvStats { get; set; } = null!;

        public DbSet<PokemonHundoStats> PokemonHundoStats { get; set; } = null!;

        public DbSet<PokemonShinyStats> PokemonShinyStats { get; set; } = null!;

        #endregion

        #endregion

        public MapDbContext(DbContextOptions<MapDbContext> options)
            : base(options)
        {
            Interlocked.Increment(ref InstanceCount);

            // Disable entity tracking for map entities for multiple reasons:
            // - It would be useful, but it's not worth the overhead and issues it could potentially introduce.
            // - Most data entities are consumable only for a certain time span.
            base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Ensure all lat/lon properties have a maximum precision of 6 decimal places
            configurationBuilder
                .Properties<double>()
                .HavePrecision(18, 6);
            configurationBuilder
                .Properties<double>()
                .HavePrecision(18, 6);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseTriggers(triggerOptions =>
            {
                triggerOptions.AddTrigger<PokemonInsertOrUpdateTrigger>();
                // TODO: RaidInsertOrUpdateTrigger
                // TODO: LureInsertOrUpdateTrigger?
                // TODO: IncidentInsertOrUpdateTrigger
                // TODO: QuestInsertOrUpdateTrigger
            });
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasCharSet("utf8mb4", DelegationModes.ApplyToAll);
            
            modelBuilder.Entity<Cell>(entity =>
            {
                /*
                entity.HasMany(c => c.Gyms)
                      .WithOne(g => g.Cell)
                      .HasForeignKey(g => g.CellId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Pokestops)
                      .WithOne(p => p.Cell)
                      .HasForeignKey(p => p.CellId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Pokemon)
                      .WithOne(p => p.Cell)
                      .HasForeignKey(p => p.CellId)
                      .OnDelete(DeleteBehavior.Cascade);
                */

                entity.HasIndex(p => p.Latitude);
                entity.HasIndex(p => p.Longitude);
            });

            modelBuilder.Entity<Gym>(entity =>
            {
                /*
                entity.HasOne(g => g.Cell)
                      .WithMany(c => c.Gyms)
                      .HasForeignKey(g => g.CellId);
                */

                entity.HasMany(g => g.Defenders)
                      .WithOne(d => d.Fort)
                      .HasForeignKey(d => d.FortId);

                entity.HasIndex(p => p.Latitude);
                entity.HasIndex(p => p.Longitude);
                entity.HasIndex(p => p.IsEnabled);
                entity.HasIndex(p => p.IsDeleted);
                entity.HasIndex(p => p.CellId);
                entity.HasIndex(p => p.Updated);
                entity.HasIndex(p => p.RaidEndTimestamp);
            });

            modelBuilder.Entity<GymDefender>(entity =>
            {
                entity.HasOne(g => g.Trainer)
                      .WithMany(t => t.Defenders)
                      .HasForeignKey(t => t.TrainerName);

                entity.HasIndex(p => p.TrainerName);
            });

            modelBuilder.Entity<GymTrainer>(entity =>
            {
                entity.HasMany(t => t.Defenders)
                      .WithOne(g => g.Trainer)
                      .HasForeignKey(p => p.TrainerName)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.Name);
            });

            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasOne(p => p.Pokestop)
                      .WithMany(p => p.Incidents)
                      .HasForeignKey(p => p.PokestopId);
                      //.HasConstraintName("FK_incident_pokestop_pokestop_id");

                entity.HasIndex(p => p.PokestopId);
                entity.HasIndex(p => p.Expiration);
            });

            modelBuilder.Entity<Pokestop>(entity =>
            {
                entity.Property(p => p.QuestConditions)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>?>(),
                           DbContextFactory.CreateValueComparer<Dictionary<string, dynamic>?>()
                       );
                entity.Property(p => p.QuestRewards)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>?>(),
                           DbContextFactory.CreateValueComparer<Dictionary<string, dynamic>?>()
                       );
                entity.Property(p => p.AlternativeQuestConditions)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>?>(),
                           DbContextFactory.CreateValueComparer<Dictionary<string, dynamic>?>()
                       );
                entity.Property(p => p.AlternativeQuestRewards)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>?>(),
                           DbContextFactory.CreateValueComparer<Dictionary<string, dynamic>?>()
                       );

                entity.Property(p => p.QuestRewardType)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].type'),'$[0]')");
                entity.Property(p => p.QuestItemId)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].info.item_id'),'$[0]')");
                entity.Property(p => p.QuestRewardAmount)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].info.amount'),'$[0]')");
                entity.Property(p => p.QuestPokemonId)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].info.pokemon_id'),'$[0]')");

                entity.Property(p => p.AlternativeQuestRewardType)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].type'),'$[0]')");
                entity.Property(p => p.AlternativeQuestItemId)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.item_id'),'$[0]')");
                entity.Property(p => p.AlternativeQuestRewardAmount)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.amount'),'$[0]')");
                entity.Property(p => p.AlternativeQuestPokemonId)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.pokemon_id'),'$[0]')");

                entity.HasMany(p => p.Incidents)
                      .WithOne(p => p.Pokestop)
                      .HasForeignKey(p => p.PokestopId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Pokemon)
                      .WithOne(p => p.Pokestop)
                      .HasForeignKey(p => p.PokestopId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(p => p.Latitude);
                entity.HasIndex(p => p.Longitude);
                entity.HasIndex(p => p.IsEnabled);
                entity.HasIndex(p => p.IsDeleted);
                entity.HasIndex(p => p.CellId);
                entity.HasIndex(p => p.Updated);

                entity.HasIndex(p => p.QuestConditions);
                entity.HasIndex(p => p.QuestRewards);
                entity.HasIndex(p => p.QuestTarget);
                entity.HasIndex(p => p.QuestTemplate);
                entity.HasIndex(p => p.QuestTimestamp);
                entity.HasIndex(p => p.QuestTitle);
                entity.HasIndex(p => p.QuestType);

                entity.HasIndex(p => p.AlternativeQuestConditions);
                entity.HasIndex(p => p.AlternativeQuestRewards);
                entity.HasIndex(p => p.AlternativeQuestTarget);
                entity.HasIndex(p => p.AlternativeQuestTemplate);
                entity.HasIndex(p => p.AlternativeQuestTimestamp);
                entity.HasIndex(p => p.AlternativeQuestTitle);
                entity.HasIndex(p => p.AlternativeQuestType);
            });

            modelBuilder.Entity<Pokemon>(entity =>
            {
                entity.Property(p => p.IV)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("(`atk_iv` + `def_iv` + `sta_iv`) * 100 / 45");
                entity.Property(p => p.SeenType)
                      .HasConversion(
                           x => Entities.Pokemon.SeenTypeToString(x),
                           x => Entities.Pokemon.StringToSeenType(x)
                       );
                entity.Property(p => p.PvpRankings)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<Dictionary<string, dynamic>?>(),
                           DbContextFactory.CreateValueComparer<string, dynamic>()
                       );

                // Ensure properties are clamped to a maximum precision of 2 decimal places
                entity.Property(p => p.Weight)
                      .HasPrecision(18, 2);
                entity.Property(p => p.Size)
                      .HasPrecision(18, 2);
                entity.Property(p => p.Capture1)
                      .HasPrecision(18, 2);
                entity.Property(p => p.Capture2)
                      .HasPrecision(18, 2);
                entity.Property(p => p.Capture3)
                      .HasPrecision(18, 2);
                entity.Property(p => p.IV)
                      .HasPrecision(18, 2);

                /*
                entity.HasOne(p => p.Cell)
                      .WithMany(c => c.Pokemon)
                      .HasForeignKey(p => p.CellId);
                */

                entity.HasOne(p => p.Pokestop)
                      .WithMany(p => p.Pokemon)
                      //.HasConstraintName("pokestop_id")
                      .HasForeignKey(p => p.PokestopId);

                /*
                entity.HasOne(p => p.Spawnpoint)
                      .WithMany(p => p.Pokemon)
                      .HasForeignKey(p => p.SpawnId);
                */

                entity.HasIndex(p => p.Latitude);
                entity.HasIndex(p => p.Longitude);
                entity.HasIndex(p => new { p.Latitude, p.Longitude }, "ix_coords");

                entity.HasIndex(p => p.AttackIV);
                entity.HasIndex(p => p.DefenseIV);
                entity.HasIndex(p => p.StaminaIV);
                entity.HasIndex(p => new { p.AttackIV, p.DefenseIV, p.StaminaIV }, "ix_iv");

                entity.HasIndex(p => p.PokemonId);
                entity.HasIndex(p => p.Level);
                entity.HasIndex(p => p.ExpireTimestamp);
                entity.HasIndex(p => p.FirstSeenTimestamp);
                entity.HasIndex(p => p.CellId);
                entity.HasIndex(p => p.PokestopId);
                entity.HasIndex(p => p.SpawnId);
                entity.HasIndex(p => p.Updated);
                entity.HasIndex(p => p.Username);
            });

            modelBuilder.Entity<Spawnpoint>(entity =>
            {
                /*
                entity.HasMany(p => p.Pokemon)
                      .WithOne(p => p.Spawnpoint)
                      .HasForeignKey(p => p.SpawnId)
                      .OnDelete(DeleteBehavior.SetNull);
                */

                entity.HasIndex(p => p.Latitude);
                entity.HasIndex(p => p.Longitude);
                entity.HasIndex(p => p.DespawnSecond);
            });

            modelBuilder.Entity<PokemonStats>(entity =>
            {
                entity.HasKey(p => new { p.Date, p.PokemonId, p.FormId });
            });

            modelBuilder.Entity<PokemonIvStats>(entity =>
            {
                entity.HasKey(p => new { p.Date, p.PokemonId, p.FormId, p.IV });
                entity.Property(p => p.IV)
                      .HasPrecision(18, 2);
            });

            modelBuilder.Entity<PokemonHundoStats>(entity =>
            {
                entity.HasKey(p => new { p.Date, p.PokemonId, p.FormId });
            });

            modelBuilder.Entity<PokemonShinyStats>(entity =>
            {
                entity.HasKey(p => new { p.Date, p.PokemonId, p.FormId });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}