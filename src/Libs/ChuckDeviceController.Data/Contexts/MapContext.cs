namespace ChuckDeviceController.Data.Contexts
{
    using System;
    using System.Collections.Generic;

    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Factories;

    public class MapContext : DbContext
    {
        public MapContext(DbContextOptions<MapContext> options)
            : base(options)
        {
            // Migrate to latest
            //var createSql = Database.GenerateCreateScript();
            //Console.WriteLine($"CreateSql: {createSql}");
            //base.Database.Migrate();

            base.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        // Map entities
        public DbSet<Gym>? Gyms { get; set; }

        public DbSet<GymDefender>? GymDefenders { get; set; }

        public DbSet<GymTrainer>? GymTrainers { get; set; }

        public DbSet<Pokemon>? Pokemon { get; set; }

        public DbSet<Pokestop>? Pokestops { get; set; }

        public DbSet<Incident>? Incidents { get; set; }

        public DbSet<Cell>? Cells { get; set; }

        public DbSet<Spawnpoint>? Spawnpoints { get; set; }

        public DbSet<Weather>? Weather { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*
            modelBuilder.Entity<Pokemon>()
                        .Property(p => p.Pvp)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<Dictionary<string, List<dynamic>>>()); // TODO: PvpRank
            */
            modelBuilder.Entity<Pokestop>()
                        .Property(nameof(Pokestop.QuestConditions))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());
            modelBuilder.Entity<Pokestop>()
                        .Property(nameof(Pokestop.QuestRewards))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());
            modelBuilder.Entity<Pokestop>()
                        .Property(nameof(Pokestop.AlternativeQuestConditions))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());
            modelBuilder.Entity<Pokestop>()
                        .Property(nameof(Pokestop.AlternativeQuestRewards))
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<List<Dictionary<string, dynamic>>>());

            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.QuestRewardType)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].type'),'$[0]')");
            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.QuestItemId)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].info.item_id'),'$[0]')");
            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.QuestRewardAmount)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].info.amount'),'$[0]')");
            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.QuestPokemonId)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`quest_rewards`,'$[*].info.pokemon_id'),'$[0]')");

            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.AlternativeQuestRewardType)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].type'),'$[0]')");
            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.AlternativeQuestItemId)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.item_id'),'$[0]')");
            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.AlternativeQuestRewardAmount)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.amount'),'$[0]')");
            modelBuilder.Entity<Pokestop>()
                        .Property(p => p.AlternativeQuestPokemonId)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("json_extract(json_extract(`alternative_quest_rewards`,'$[*].info.pokemon_id'),'$[0]')");

            modelBuilder.Entity<Pokemon>()
                        .Property(p => p.IV)
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("(`atk_iv` + `def_iv` + `sta_iv`) * 100 / 45");
            modelBuilder.Entity<Pokemon>()
                        .Property(p => p.SeenType)
                        .HasConversion(x => Entities.Pokemon.SeenTypeToString(x), x => Entities.Pokemon.StringToSeenType(x));
            modelBuilder.Entity<Pokemon>()
                        .Property(p => p.PvpRankings)
                        .HasConversion(DbContextFactory.CreateJsonValueConverter<Dictionary<string, dynamic>>());

            base.OnModelCreating(modelBuilder);
        }
    }
}