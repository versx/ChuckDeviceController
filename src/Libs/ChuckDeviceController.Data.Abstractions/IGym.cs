namespace ChuckDeviceController.Data.Abstractions;

public interface IGym : IBaseEntity
{
    string Id { get; }

    string? Name { get; }

    string? Url { get; }

    double Latitude { get; }

    double Longitude { get; }

    ulong LastModifiedTimestamp { get; }

    ulong? RaidEndTimestamp { get; }

    ulong? RaidSpawnTimestamp { get; }

    ulong? RaidBattleTimestamp { get; }

    ulong Updated { get; }

    uint? RaidPokemonId { get; }

    uint GuardingPokemonId { get; }

    ushort AvailableSlots { get; }

    // TODO: Team Team { get; }

    ushort? RaidLevel { get; }

    bool IsEnabled { get; }

    bool IsExRaidEligible { get; }

    bool InBattle { get; }

    uint? RaidPokemonMove1 { get; }

    uint? RaidPokemonMove2 { get; }

    uint? RaidPokemonForm { get; }

    uint? RaidPokemonCostume { get; }

    uint? RaidPokemonCP { get; }

    uint? RaidPokemonEvolution { get; }

    ushort? RaidPokemonGender { get; }

    bool? RaidIsExclusive { get; }

    ulong CellId { get; }

    bool IsDeleted { get; }

    int TotalCP { get; }

    ulong FirstSeenTimestamp { get; }

    uint? SponsorId { get; }

    bool? IsArScanEligible { get; }

    uint? PowerUpPoints { get; }

    ushort? PowerUpLevel { get; }

    ulong? PowerUpEndTimestamp { get; }
}