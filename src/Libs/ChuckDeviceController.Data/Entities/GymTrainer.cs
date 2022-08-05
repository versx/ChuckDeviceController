namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Common.Data.Contracts;

    [Table("gym_trainer")]
    public class GymTrainer : BaseEntity, IGymTrainer
    {
        #region Properties

        [
            Column("name"),
            DatabaseGenerated(DatabaseGeneratedOption.None),
            Key,
        ]
        public string Name { get; set; }

        [Column("level")]
        public ushort Level { get; set; }

        [Column("team_id")]
        public Team TeamId { get; set; }

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
        public ulong CombatRating { get; set; }

        [Column("has_shared_ex_pass")]
        public bool HasSharedExPass { get; set; }

        [Column("gym_badge_type")]
        public ushort GymBadgeType { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        #endregion

        #region Constructors

        public GymTrainer()
        {
        }

        public GymTrainer(PlayerPublicProfileProto profileData)
        {
            //profileData.Badges[0]
            //profileData.TimedGroupChallengeStats.Challenges[0].
            Name = profileData.Name;
            Level = Convert.ToUInt16(profileData.Level);
            TeamId = profileData.Team;
            BattlesWon = Convert.ToUInt32(profileData.BattlesWon);
            KmWalked = profileData.KmWalked;
            PokemonCaught = Convert.ToUInt64(profileData.CaughtPokemon);
            Experience = Convert.ToUInt64(profileData.Experience);
            CombatRank = Convert.ToUInt64(profileData.CombatRank);
            CombatRating = Convert.ToUInt64(profileData.CombatRating);
            HasSharedExPass = profileData.HasSharedExPass;
            GymBadgeType = Convert.ToUInt16(profileData.GymBadgeType);
        }

        #endregion
    }
}