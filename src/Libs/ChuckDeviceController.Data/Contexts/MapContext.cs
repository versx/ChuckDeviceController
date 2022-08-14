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
            modelBuilder.Entity<Gym>(entity =>
            {
                entity.HasIndex(p => p.CellId);
            });

            modelBuilder.Entity<GymDefender>(entity =>
            {
                entity.HasIndex(p => p.TrainerName);
            });

            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasIndex(p => p.PokestopId);
            });

            modelBuilder.Entity<Pokestop>(entity =>
            {
                entity.Property(nameof(Pokestop.QuestConditions))
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());
                entity.Property(nameof(Pokestop.QuestRewards))
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());
                entity.Property(nameof(Pokestop.AlternativeQuestConditions))
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());
                entity.Property(nameof(Pokestop.AlternativeQuestRewards))
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());

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
            });

            modelBuilder.Entity<Pokemon>(entity =>
            {
                entity.Property(p => p.IV)
                      .ValueGeneratedOnAddOrUpdate()
                      .HasComputedColumnSql("(`atk_iv` + `def_iv` + `sta_iv`) * 100 / 45");
                entity.Property(p => p.SeenType)
                      .HasConversion(x => Entities.Pokemon.SeenTypeToString(x), x => Entities.Pokemon.StringToSeenType(x));
                entity.Property(nameof(Entities.Pokemon.PvpRankings))
                      .HasConversion(DbContextFactory.CreateJsonValueConverter<Dictionary<string, dynamic>>());

                entity.HasIndex(p => p.CellId);
                entity.HasIndex(p => p.SpawnId);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}