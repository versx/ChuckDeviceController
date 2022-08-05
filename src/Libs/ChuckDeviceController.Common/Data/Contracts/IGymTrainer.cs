namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IGymTrainer : IBaseEntity
    {
        string Name { get; }

        ushort Level { get; }

        // TODO: Team TeamId { get; }

        uint BattlesWon { get; }

        double KmWalked { get; }

        ulong PokemonCaught { get; }

        ulong Experience { get; }

        ulong CombatRank { get; }

        ulong CombatRating { get; }

        bool HasSharedExPass { get; }

        ushort GymBadgeType { get; }

        ulong Updated { get; }
    }
}