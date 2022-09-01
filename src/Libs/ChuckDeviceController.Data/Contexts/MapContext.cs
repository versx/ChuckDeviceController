namespace ChuckDeviceController.Data.Contexts
{
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;
    using ChuckDeviceController.Data.Triggers;

    public class MapContext : DbContext
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MapContext(DbContextOptions<MapContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {
            base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseTriggers(triggerOptions =>
            {
                triggerOptions.AddTrigger<PokemonInsertedTrigger>();
            });
            base.OnConfiguring(optionsBuilder);
        }

        // Map entities
        public DbSet<Gym> Gyms { get; set; }

        public DbSet<GymDefender> GymDefenders { get; set; }

        public DbSet<GymTrainer> GymTrainers { get; set; }

        public DbSet<Pokemon> Pokemon { get; set; }

        public DbSet<Pokestop> Pokestops { get; set; }

        public DbSet<Incident> Incidents { get; set; }

        public DbSet<Cell> Cells { get; set; }

        public DbSet<Spawnpoint> Spawnpoints { get; set; }

        public DbSet<Weather> Weather { get; set; }

        #region Pokemon Statistics

        public DbSet<PokemonStats> PokemonStats { get; set; }

        public DbSet<PokemonIvStats> PokemonIvStats { get; set; }

        public DbSet<PokemonHundoStats> PokemonHundoStats { get; set; }

        public DbSet<PokemonShinyStats> PokemonShinyStats { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
            modelBuilder.Entity<Cell>(entity =>
            {
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
            });
            */

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

                entity.HasIndex(p => p.CellId);
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

                entity.HasIndex(p => p.CellId);
            });

            modelBuilder.Entity<Pokemon>(entity =>
            {
                entity.Property(p => p.IV)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("(`atk_iv` + `def_iv` + `sta_iv`) * 100 / 45");
                entity.Property(p => p.SeenType)
                      .HasConversion(x => Entities.Pokemon.SeenTypeToString(x), x => Entities.Pokemon.StringToSeenType(x));
                entity.Property(p => p.PvpRankings)
                      .HasConversion(
                           DbContextFactory.CreateJsonValueConverter<Dictionary<string, dynamic>?>(),
                           DbContextFactory.CreateValueComparer<string, dynamic>()
                       );

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

                entity.HasIndex(p => p.CellId);
                entity.HasIndex(p => p.PokestopId);
                entity.HasIndex(p => p.SpawnId);
            });

            /*
            modelBuilder.Entity<Spawnpoint>(entity =>
            {
                entity.HasMany(p => p.Pokemon)
                      .WithOne(p => p.Spawnpoint)
                      .HasForeignKey(p => p.SpawnId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
            */

            modelBuilder.Entity<PokemonStats>(entity =>
            {
                entity.HasKey(p => new { p.Date, p.PokemonId, p.FormId });
            });

            modelBuilder.Entity<PokemonIvStats>(entity =>
            {
                entity.HasKey(p => new { p.Date, p.PokemonId, p.FormId });
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