namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    using Chuck.Infrastructure.Data.Interfaces;
    using Chuck.Infrastructure.Extensions;

    [Table("trainer")]
    public class Trainer : BaseEntity, IAggregateRoot
    {
        [
            Column("name"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("id"),
        ]
        public string Name { get; set; }

        [
            Column("level"),
            JsonPropertyName("level"),
        ]
        public ushort Level { get; set; }

        [
            Column("team_id"),
            JsonPropertyName("team_id"),
        ]
        public ushort TeamId { get; set; }

        [
            Column("battles_won"),
            JsonPropertyName("battles_won"),
        ]
        public uint BattlesWon { get; set; }

        [
            Column("km_walked"),
            JsonPropertyName("km_walked"),
        ]
        public double KmWalked { get; set; }

        [
            Column("pokemon_caught"),
            JsonPropertyName("pokemon_caught"),
        ]
        public ulong PokemonCaught { get; set; }

        [
            Column("experience"),
            JsonPropertyName("experience"),
        ]
        public ulong Experience { get; set; }

        [
            Column("combat_rank"),
            JsonPropertyName("combat_rank"),
        ]
        public ulong CombatRank { get; set; }

        [
            Column("combat_rating"),
            JsonPropertyName("combat_rating"),
        ]
        public double CombatRating { get; set; }

        [
            Column("updated"),
            JsonPropertyName("updated"),
        ]
        public ulong Updated { get; set; }

        public Trainer()
        {
        }

        public Trainer(GymDefenderProto proto)
        {
            Name = proto.TrainerPublicProfile.Name;
            Level = (ushort)proto.TrainerPublicProfile.Level;
            TeamId = (ushort)proto.TrainerPublicProfile.Team;
            BattlesWon = (uint)(proto.TrainerPublicProfile?.BattlesWon ?? 0);
            KmWalked = proto.TrainerPublicProfile?.KmWalked ?? 0;
            PokemonCaught = (ulong)(proto.TrainerPublicProfile?.CaughtPokemon ?? 0);
            Experience = (ulong)(proto.TrainerPublicProfile?.Experience ?? 0);
            CombatRank = (ulong)(proto.TrainerPublicProfile?.CombatRank ?? 0);
            CombatRating = proto.TrainerPublicProfile?.CombatRating ?? 0;
            Updated = DateTime.UtcNow.ToTotalSeconds();

            // TODO: New gym trainer properties
            //trainerProfile.GymBadgeType (gym badge type)
            //trainerProfile.HasSharedExPass (invited to ex raid)
        }
    }
}