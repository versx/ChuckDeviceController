namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    using Chuck.Infrastructure.Data.Interfaces;
    using Chuck.Infrastructure.Extensions;

    [Table("gym_defender")]
    public class GymDefender : BaseEntity, IAggregateRoot
    {
        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("id"),
        ]
        public string Id { get; set; }

        [
            Column("pokemon_id"),
            JsonPropertyName("pokemon_id"),
        ]
        public ushort PokemonId { get; set; }

        [
            Column("cp_when_deployed"),
            JsonPropertyName("cp_when_deployed"),
        ]
        public uint CpWhenDeployed { get; set; }

        [
            Column("cp_now"),
            JsonPropertyName("cp_now"),
        ]
        public uint CpNow { get; set; }

        [
            Column("berry_value"),
            JsonPropertyName("berry_value"),
        ]
        public double BerryValue { get; set; }

        [
            Column("times_fed"),
            JsonPropertyName("times_fed"),
        ]
        public ushort TimesFed { get; set; }

        [
            Column("deployment_duration"),
            JsonPropertyName("deployment_duration"),
        ]
        public uint DeploymentDuration { get; set; }

        [
            Column("trainer_name"),
            JsonPropertyName("trainer_name"),
        ]
        public string TrainerName { get; set; }

        [
            Column("fort_id"),
            JsonPropertyName("fort_id"),
        ]
        public string FortId { get; set; }

        [
            Column("atk_iv"),
            JsonPropertyName("atk_iv"),
        ]
        public ushort AttackIV { get; set; }

        [
            Column("def_iv"),
            JsonPropertyName("def_iv"),
        ]
        public ushort DefenseIV { get; set; }

        [
            Column("sta_iv"),
            JsonPropertyName("sta_iv"),
        ]
        public ushort StaminaIV { get; set; }

        [
            Column("move_1"),
            JsonPropertyName("move_1"),
        ]
        public ushort Move1 { get; set; }

        [
            Column("move_2"),
            JsonPropertyName("move_2"),
        ]
        public ushort Move2 { get; set; }

        [
            Column("battles_attacked"),
            JsonPropertyName("battles_attacked"),
        ]
        public ushort BattlesAttacked { get; set; }

        [
            Column("battles_defended"),
            JsonPropertyName("battles_defended"),
        ]
        public ushort BattlesDefended { get; set; }

        [
            Column("gender"),
            JsonPropertyName("gender"),
        ]
        public ushort Gender { get; set; }

        [
            Column("hatched_from_egg"),
            JsonPropertyName("hatched_from_egg"),
        ]
        public bool HatchedFromEgg { get; set; }

        [
            Column("pvp_combat_won"),
            JsonPropertyName("pvp_combat_won"),
        ]
        public ushort PvpCombatWon { get; set; }

        [
            Column("pvp_combat_total"),
            JsonPropertyName("pvp_combat_total"),
        ]
        public ushort PvpCombatTotal { get; set; }

        [
            Column("npc_combat_won"),
            JsonPropertyName("npc_combat_won"),
        ]
        public ushort NpcCombatWon { get; set; }

        [
            Column("npc_combat_total"),
            JsonPropertyName("npc_combat_total"),
        ]
        public ushort NpcCombatTotal { get; set; }

        [
            Column("updated"),
            JsonPropertyName("updated"),
        ]
        public ulong Updated { get; set; }

        public GymDefender(string fortId, GymDefenderProto proto)
        {
            Id = proto.MotivatedPokemon.Pokemon.Id.ToString(); // TODO: Convert to ulong
            PokemonId = (ushort)proto.MotivatedPokemon.Pokemon.PokemonId;
            CpWhenDeployed = (uint)proto.MotivatedPokemon.CpWhenDeployed;
            CpNow = (uint)proto.MotivatedPokemon.CpNow;
            BerryValue = proto.MotivatedPokemon.BerryValue;
            TimesFed = (ushort)proto.DeploymentTotals?.TimesFed;
            DeploymentDuration = (uint)proto.DeploymentTotals?.DeploymentDurationMs / 1000;
            TrainerName = proto.MotivatedPokemon.Pokemon.OwnerName;
            FortId = fortId;
            AttackIV = (ushort)proto.MotivatedPokemon.Pokemon?.IndividualAttack;
            DefenseIV = (ushort)proto.MotivatedPokemon.Pokemon?.IndividualDefense;
            StaminaIV = (ushort)proto.MotivatedPokemon.Pokemon?.IndividualStamina;
            Move1 = (ushort)proto.MotivatedPokemon.Pokemon?.Move1;
            Move2 = (ushort)proto.MotivatedPokemon.Pokemon?.Move2;
            BattlesAttacked = (ushort)proto.MotivatedPokemon.Pokemon.BattlesAttacked;
            BattlesDefended = (ushort)proto.MotivatedPokemon.Pokemon.BattlesDefended;
            Gender = (ushort)proto.MotivatedPokemon.Pokemon.PokemonDisplay.Gender;
            HatchedFromEgg = proto.MotivatedPokemon.Pokemon.HatchedFromEgg;
            PvpCombatWon = (ushort)(proto.MotivatedPokemon.Pokemon.PvpCombatStats?.NumWon ?? 0);
            PvpCombatTotal = (ushort)(proto.MotivatedPokemon.Pokemon.PvpCombatStats?.NumTotal ?? 0);
            NpcCombatWon = (ushort)(proto.MotivatedPokemon.Pokemon.NpcCombatStats?.NumWon ?? 0);
            NpcCombatTotal = (ushort)(proto.MotivatedPokemon.Pokemon.NpcCombatStats?.NumTotal ?? 0);
            Updated = DateTime.UtcNow.ToTotalSeconds();

            // TODO: New gym defender properties
            //BuddyCandyAwarded
            //BuddyKmWalked
            //DisplayPokemonId
            //Favorite
            //Form
            //EvolutionQuestInfo ??
            //HasMegaEvolved
            //HeightM
            //IsBad
            //IsEgg
            //IsLucky
            //Move3
            //Nickname
            //OriginDetail
            //OriginalOwnerNickname,
            //Pokeball
            //PokemonDisplay.Form
            //PokemonDisplay.Costume
            //TradedTimeMs
            //WeightKg
        }
    }
}