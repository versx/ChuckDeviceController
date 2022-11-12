namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IPokemon : IBaseEntity
    {
        string Id { get; }

        uint PokemonId { get; }

        double Latitude { get; }

        double Longitude { get; }

        ulong? SpawnId { get; }

        ulong ExpireTimestamp { get; }

        ushort? AttackIV { get; }

        ushort? DefenseIV { get; }

        ushort? StaminaIV { get; }

        double? IV { get; }

        ushort? Move1 { get; }

        ushort? Move2 { get; }

        ushort? Gender { get; }

        ushort? Form { get; }

        ushort? Costume { get; }

        ushort? CP { get; }

        ushort? Level { get; }

        double? Weight { get; }

        double? Size { get; }

        ushort? Weather { get; }

        bool? IsShiny { get; }

        string? Username { get; }

        string? PokestopId { get; }

        ulong? FirstSeenTimestamp { get; }

        ulong Updated { get; }

        ulong Changed { get; }

        ulong CellId { get; }

        bool IsExpireTimestampVerified { get; }

        double? Capture1 { get; }

        double? Capture2 { get; }

        double? Capture3 { get; }

        bool IsDitto { get; }

        uint? DisplayPokemonId { get; }

        Dictionary<string, dynamic>? PvpRankings { get; }

        double BaseHeight { get; }

        double BaseWeight { get; }

        bool IsEvent { get; }

        SeenType SeenType { get; }
    }
}