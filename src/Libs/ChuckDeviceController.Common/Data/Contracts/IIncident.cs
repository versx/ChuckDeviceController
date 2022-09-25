namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IIncident : IBaseEntity
    {
        string Id { get; }

        string? PokestopId { get; }

        ulong Start { get; }

        ulong Expiration { get; }

        uint DisplayType { get; }

        uint Style { get; }

        ushort Character { get; }

        ulong Updated { get; }
    }
}