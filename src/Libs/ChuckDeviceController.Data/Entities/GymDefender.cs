namespace ChuckDeviceController.Data.Entities;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using POGOProtos.Rpc;

using ChuckDeviceController.Common;
using ChuckDeviceController.Data.Abstractions;
using ChuckDeviceController.Extensions;

[Table("gym_defender")]
public class GymDefender : BaseEntity, IGymDefender, IWebhookEntity
{
    #region Properties

    [
        Column("id"),
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.None),
    ]
    public ulong Id { get; set; }

    [Column("nickname")]
    public string? Nickname { get; set; } = null!;

    [Column("pokemon_id")]
    public ushort PokemonId { get; set; }

    [Column("display_pokemon_id")]
    public ushort DisplayPokemonId { get; set; }

    [Column("form")]
    public ushort Form { get; set; }

    [Column("costume")]
    public ushort Costume { get; set; }

    [Column("gender")]
    public ushort Gender { get; set; }

    [Column("cp_when_deployed")]
    public uint CpWhenDeployed { get; set; }

    [Column("cp_now")]
    public uint CpNow { get; set; }

    [Column("cp")]
    public uint Cp { get; set; }

    [Column("battles_won")]
    public uint BattlesWon { get; set; }

    [Column("battles_lost")]
    public uint BattlesLost { get; set; }

    [
        Column("berry_value"),
        Precision(18, 2),
    ]
    public double BerryValue { get; set; }

    [Column("times_fed")]
    public uint TimesFed { get; set; }

    [Column("deployment_duration")]
    public ulong DeploymentDuration { get; set; }

    [
        Column("trainer_name"),
        ForeignKey("trainer_name"),
    ]
    public string? TrainerName { get; set; }

    public virtual GymTrainer? Trainer { get; set; }

    [
        Column("fort_id"),
        ForeignKey("fort_id"),
    ]
    public string? FortId { get; set; }

    public virtual Gym? Fort { get; set; }

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

    [Column("move_3")]
    public ushort Move3 { get; set; }

    [Column("battles_attacked")]
    public uint BattlesAttacked { get; set; }

    [Column("battles_defended")]
    public uint BattlesDefended { get; set; }

    [Column("buddy_km_walked")]
    public double BuddyKmWalked { get; set; }

    [Column("buddy_candy_awarded")]
    public uint BuddyCandyAwarded { get; set; }

    [Column("coins_returned")]
    public uint CoinsReturned { get; set; }

    [Column("from_fort")]
    public bool FromFort { get; set; }

    [Column("hatched_from_egg")]
    public bool HatchedFromEgg { get; set; }

    [Column("is_bad")]
    public bool IsBad { get; set; }

    [Column("is_egg")]
    public bool IsEgg { get; set; }

    [Column("is_lucky")]
    public bool IsLucky { get; set; }

    [Column("shiny")]
    public bool IsShiny { get; set; }

    [Column("pvp_combat_won")]
    public uint PvpCombatWon { get; set; }

    [Column("pvp_combat_total")]
    public uint PvpCombatTotal { get; set; }

    [Column("npc_combat_won")]
    public uint NpcCombatWon { get; set; }

    [Column("npc_combat_total")]
    public uint NpcCombatTotal { get; set; }

    [
        Column("height_m"),
        Precision(18, 2),
    ]
    public double HeightM { get; set; }

    [
        Column("weight_kg"),
        Precision(18, 2),
    ]
    public double WeightKg { get; set; }

    [Column("updated")]
    public ulong Updated { get; set; }

    #endregion

    #region Constructors

    public GymDefender()
    {
        Nickname = string.Empty;
    }

    public GymDefender(GymDefenderProto gymDefenderData, string fortId)
    {
        var pokemon = gymDefenderData.MotivatedPokemon;
        var deployment = gymDefenderData.DeploymentTotals;
        Id = pokemon.Pokemon.Id;
        Nickname = pokemon.Pokemon.Nickname;
        PokemonId = Convert.ToUInt16(pokemon.Pokemon.PokemonId);
        DisplayPokemonId = Convert.ToUInt16(pokemon.Pokemon.DisplayPokemonId);
        Form = Convert.ToUInt16(pokemon.Pokemon.PokemonDisplay.Form);
        Costume = Convert.ToUInt16(pokemon.Pokemon.PokemonDisplay.Costume);
        Gender = Convert.ToUInt16(pokemon.Pokemon.PokemonDisplay.Gender);
        CpWhenDeployed = Convert.ToUInt32(pokemon.CpWhenDeployed);
        CpNow = Convert.ToUInt32(pokemon.CpNow);
        Cp = Convert.ToUInt32(pokemon.Pokemon?.Cp ?? 0);
        BerryValue = pokemon.BerryValue;
        BuddyKmWalked = pokemon.Pokemon?.BuddyKmWalked ?? 0;
        BuddyCandyAwarded = Convert.ToUInt32(pokemon.Pokemon?.BuddyCandyAwarded ?? 0);
        CoinsReturned = Convert.ToUInt32(pokemon.Pokemon?.CoinsReturned ?? 0);
        BattlesWon = Convert.ToUInt32(deployment.BattlesWon);
        BattlesLost = Convert.ToUInt32(deployment.BattlesLost);
        TimesFed = Convert.ToUInt32(deployment?.TimesFed);
        DeploymentDuration = Convert.ToUInt64(deployment?.DeploymentDurationMs / 1000);
        //Favorite = pokemon.Pokemon.Favorite;
        FromFort = pokemon.Pokemon?.FromFort ?? false;
        IsBad = pokemon.Pokemon?.IsBad ?? false;
        IsEgg = pokemon.Pokemon?.IsEgg ?? false;
        IsLucky = pokemon.Pokemon?.IsLucky ?? false;
        IsShiny = pokemon.Pokemon?.PokemonDisplay?.Shiny ?? false;
        HeightM = pokemon.Pokemon?.HeightM ?? 0;
        WeightKg = pokemon.Pokemon?.WeightKg ?? 0;
        HatchedFromEgg = pokemon.Pokemon?.HatchedFromEgg ?? false;
        TrainerName = pokemon.Pokemon?.OwnerName ?? null;
        FortId = fortId;
        AttackIV = Convert.ToUInt16(pokemon.Pokemon?.IndividualAttack);
        DefenseIV = Convert.ToUInt16(pokemon.Pokemon?.IndividualDefense);
        StaminaIV = Convert.ToUInt16(pokemon.Pokemon?.IndividualStamina);
        Move1 = Convert.ToUInt16(pokemon.Pokemon?.Move1 ?? 0);
        Move2 = Convert.ToUInt16(pokemon.Pokemon?.Move2 ?? 0);
        Move3 = Convert.ToUInt16(pokemon.Pokemon?.Move3 ?? 0);
        BattlesAttacked = Convert.ToUInt32(pokemon.Pokemon?.BattlesAttacked ?? 0);
        BattlesDefended = Convert.ToUInt32(pokemon.Pokemon?.BattlesDefended ?? 0);
        PvpCombatWon = Convert.ToUInt32(pokemon.Pokemon?.PvpCombatStats?.NumWon ?? 0);
        PvpCombatTotal = Convert.ToUInt32(pokemon.Pokemon?.PvpCombatStats?.NumTotal ?? 0);
        NpcCombatWon = Convert.ToUInt32(pokemon.Pokemon?.NpcCombatStats?.NumWon ?? 0);
        NpcCombatTotal = Convert.ToUInt32(pokemon.Pokemon?.NpcCombatStats?.NumTotal ?? 0);
        Updated = DateTime.UtcNow.ToTotalSeconds();

        //pokemon.DeployMs
        //pokemon.FeedCooldownDurationMillis
        //pokemon.FoodValue
        //pokemon.MotivationNow
        //pokemon.Pokemon.AdditionalCpMultiplier
        //pokemon.Pokemon.CapturedS2CellId
        //pokemon.Pokemon.CpMultiplier
        //pokemon.Pokemon.CpMultiplierBeforeTrading
        //pokemon.Pokemon.DeployedFortId
        //pokemon.Pokemon.DisplayCp
        //pokemon.Pokemon.Favorite
        //pokemon.Pokemon.LimitedPokemonIdentifier
        //pokemon.Pokemon.MaxStamina
        //pokemon.Pokemon.MegaEvolvedForms
        //pokemon.Pokemon.Move2IsPurifiedExclusive
        //pokemon.Pokemon.NumUpgrades
        //pokemon.Pokemon.OriginalOwnerNickname
        //pokemon.Pokemon.OriginDetail.RaidDetail
        //pokemon.Pokemon.OriginDetail.WildDetail
        //pokemon.Pokemon.OriginDetail.TutorialDetail
        //pokemon.Pokemon.Pokeball
        //pokemon.Pokemon.PokemonDisplay.DisplayId
        //pokemon.Pokemon.PokemonDisplay.Alignment
        //pokemon.Pokemon.PokemonDisplay.PokemonBadge
        //pokemon.Pokemon.PokemonDisplay.CurrentTempEvolution
        //pokemon.Pokemon.PokemonDisplay.LockedTempEvolution
        //pokemon.Pokemon.PokemonDisplay.MegaEvolutionLevel
        //pokemon.Pokemon.PokemonDisplay.OriginalCostume
        //pokemon.Pokemon.PokemonDisplay.TempEvolutionIsLocked
        //pokemon.Pokemon.PokemonDisplay.TemporaryEvolutionFinishMs
        //pokemon.Pokemon.PokemonDisplay.WeatherBoostedCondition
        //pokemon.Pokemon.PreBoostedAdditionalCpMultiplier
        //pokemon.Pokemon.PreBoostedCp
        //pokemon.Pokemon.Stamina
        //pokemon.Pokemon.TempEvoCp
        //pokemon.Pokemon.TempEvoCpMultiplier
        //pokemon.Pokemon.TempEvoStaminaModifier
        //pokemon.Pokemon.TradedTimeMs
        //pokemon.Pokemon.TradingOriginalOwnerHash
    }

    #endregion

    #region Public Methods

    public dynamic? GetWebhookData(string type)
    {
        throw new NotImplementedException();
    }

    public dynamic? GetWebhookData(string type, Gym gym)
    {
        return type.ToLower() switch
        {
            "gym-defender" or _ => new
            {
                type = WebhookHeaders.GymDefender,
                message = new
                {
                    id = Id,
                    nickname = Nickname,
                    trainer_name = TrainerName,
                    move_1 = Move1,
                    move_2 = Move2,
                    move_3 = Move3,
                    fort_id = FortId,
                    gym_id = FortId,
                    gym_name = gym?.Name ?? Gym.UnknownGymName,
                    gym_url = gym?.Url,
                    latitude = gym?.Latitude,
                    longitude = gym?.Longitude,
                    pokemon_id = PokemonId,
                    form = Form,
                    costume = Costume,
                    gender = Gender,
                    individual_attack = AttackIV,
                    individual_defense = DefenseIV,
                    individual_stamina = StaminaIV,
                    cp = Cp,
                    cp_now = CpNow,
                    cp_when_deployed = CpWhenDeployed,
                    coins_returned = CoinsReturned,
                    times_fed = TimesFed,
                    berry_value = BerryValue,
                    deployment_duration = DeploymentDuration,
                    display_pokemon_id = DisplayPokemonId,
                    from_raid = FromFort,
                    from_fort = FromFort,
                    hatched_from_egg = HatchedFromEgg,
                    is_egg = IsEgg,
                    is_shiny = IsShiny,
                    is_bad = IsBad,
                    is_striked = IsBad,
                    is_lucky = IsLucky,
                    buddy = new
                    {
                        candy_awarded = BuddyCandyAwarded,
                        km_walked = BuddyKmWalked,
                    },
                    battles = new
                    {
                        attacked = BattlesAttacked,
                        defended = BattlesDefended,
                        won = BattlesWon,
                        lost = BattlesLost,
                    },
                    pvp_combat = new
                    {
                        won = PvpCombatWon,
                        total = PvpCombatTotal,
                    },
                    npm_combat = new
                    {
                        won = NpcCombatWon,
                        total = NpcCombatTotal,
                    },
                    height_m = HeightM,
                    weight_kb = WeightKg,
                    updated = Updated,
                },
            },
        };
    }

    #endregion
}