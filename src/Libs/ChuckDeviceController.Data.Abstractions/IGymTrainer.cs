namespace ChuckDeviceController.Data.Abstractions;

public interface IGymTrainer : IBaseEntity
{
    string Name { get; }

    ushort Level { get; }

    ushort TeamId { get; }

    uint BattlesWon { get; }

    double KmWalked { get; }

    ulong PokemonCaught { get; }

    ulong Experience { get; }

    uint CombatRank { get; }

    double CombatRating { get; }

    bool HasSharedExPass { get; }

    ushort GymBadgeType { get; }

    ulong Updated { get; }
}