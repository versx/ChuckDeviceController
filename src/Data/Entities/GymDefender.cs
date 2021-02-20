namespace ChuckDeviceController.Data.Entities
{
    using ChuckDeviceController.Data.Interfaces;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("gym_defender")]
    public class GymDefender : BaseEntity, IAggregateRoot
    {
        [
            Column("id"),
            Key,
        ]
        public string Id { get; set; }

        [Column("pokemon_id")]
        public ushort PokemonId { get; set; }

        [Column("cp_when_deployed")]
        public uint CpWhenDeployed { get; set; }

        [Column("cp_now")]
        public uint CpNow { get; set; }

        [Column("berry_value")]
        public double BerryValue { get; set; }

        [Column("times_fed")]
        public ushort TimesFed { get; set; }

        [Column("deployment_duration")]
        public uint DeploymentDuration { get; set; }

        [Column("trainer_name")]
        public string TrainerName { get; set; }

        [Column("fort_id")]
        public string FortId { get; set; }

        [Column("atk_iv")]
        public ushort AttackIV { get; set; }

        [Column("def_iv")]
        public ushort DefenseIV { get; set; }

        [Column("sta_iv")]
        public ushort StaminaIV { get; set; }

        [Column("move_1")]
        public ushort Move1 { get; set; }

        [Column("move_2")]
        public ushort Move2 { get; set; }

        [Column("battles_attacked")]
        public ushort BattlesAttacked { get; set; }

        [Column("battles_defended")]
        public ushort BattlesDefended { get; set; }

        [Column("gender")]
        public ushort Gender { get; set; }

        [Column("hatched_from_egg")]
        public bool HatchedFromEgg { get; set; }

        [Column("pvp_combat_won")]
        public ushort PvpCombatWon { get; set; }

        [Column("pvp_combat_total")]
        public ushort PvpCombatTotal { get; set; }

        [Column("npc_combat_won")]
        public ushort NpcCombatWon { get; set; }

        [Column("npc_combat_total")]
        public ushort NpcCombatTotal { get; set; }
    }
}