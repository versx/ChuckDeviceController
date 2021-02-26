namespace Chuck.Infrastructure.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Chuck.Infrastructure.Data.Interfaces;

    [Table("trainer")]
    public class Trainer : BaseEntity, IAggregateRoot
    {
        [
            Column("name"),
            Key,
        ]
        public string Name { get; set; }

        [Column("level")]
        public ushort Level { get; set; }

        [Column("team_id")]
        public ushort TeamId { get; set; }

        [Column("battles_won")]
        public uint BattlesWon { get; set; }

        [Column("km_walked")]
        public double KmWalked { get; set; }

        [Column("pokemon_caught")]
        public ulong PokemonCaught { get; set; }

        [Column("experience")]
        public ulong Experience { get; set; }

        [Column("combat_rank")]
        public ulong CombatRank { get; set; }

        [Column("combat_rating")]
        public double CombatRating { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }
    }
}