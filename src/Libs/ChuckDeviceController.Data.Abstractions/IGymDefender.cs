namespace ChuckDeviceController.Data.Abstractions;

public interface IGymDefender : IBaseEntity
{
    ulong Id { get; }

    string Nickname { get; }

    ushort PokemonId { get; }

    ushort DisplayPokemonId { get; }

    ushort Form { get; }

    ushort Costume { get; }

    ushort Gender { get; }

    uint CpWhenDeployed { get; }

    uint CpNow { get; }

    uint Cp { get; }

    uint BattlesWon { get; }

    uint BattlesLost { get; }

    double BerryValue { get; }

    uint TimesFed { get; }

    ulong DeploymentDuration { get; }

    string? TrainerName { get; }

    string? FortId { get; }

    ushort AttackIV { get; }

    ushort DefenseIV { get; }

    ushort StaminaIV { get; }

    ushort Move1 { get; }

    ushort Move2 { get; }

    ushort Move3 { get; }

    uint BattlesAttacked { get; }

    uint BattlesDefended { get; }

    double BuddyKmWalked { get; }

    uint BuddyCandyAwarded { get; }

    uint CoinsReturned { get; }

    bool FromFort { get; }

    bool HatchedFromEgg { get; }

    bool IsBad { get; }

    bool IsEgg { get; }

    bool IsLucky { get; }

    bool IsShiny { get; }

    uint PvpCombatWon { get; }

    uint PvpCombatTotal { get; }

    uint NpcCombatWon { get; }

    uint NpcCombatTotal { get; }

    double HeightM { get; }

    double WeightKg { get; }

    ulong Updated { get; }
}