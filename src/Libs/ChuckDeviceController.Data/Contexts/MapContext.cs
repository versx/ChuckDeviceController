namespace ChuckDeviceController.Data.Contexts
{
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;

    public class MapContext : DbContext
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MapContext(DbContextOptions<MapContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {
            base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cell>(entity =>
            {
                entity.HasMany(c => c.Gyms)
                      .WithOne(g => g.Cell)
                      .HasForeignKey(g => g.CellId);

                entity.HasMany(c => c.Pokestops)
                      .WithOne(p => p.Cell)
                      .HasForeignKey(p => p.CellId);

                entity.HasMany(c => c.Pokemon)
                      .WithOne(p => p.Cell)
                      .HasForeignKey(p => p.CellId);
            });

            modelBuilder.Entity<Gym>(entity =>
            {
                entity.HasOne(g => g.Cell)
                      .WithMany(c => c.Gyms)
                      .HasForeignKey(g => g.CellId);
                entity.HasIndex(p => p.CellId);
            });

            modelBuilder.Entity<GymDefender>(entity =>
            {
                entity.HasIndex(p => p.TrainerName);
            });

            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasOne(p => p.Pokestop)
                      .WithMany(p => p.Incidents)
                      .HasForeignKey(p => p.PokestopId);
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

                entity.HasIndex(p => p.CellId);

                entity.HasMany(p => p.Incidents)
                      .WithOne(p => p.Pokestop)
                      .HasForeignKey(p => p.PokestopId);
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

                entity.HasIndex(p => p.CellId);
                entity.HasIndex(p => p.SpawnId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}