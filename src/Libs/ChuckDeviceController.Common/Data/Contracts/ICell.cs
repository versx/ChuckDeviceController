namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface ICell : IBaseEntity
    {
        ulong Id { get; }

        ushort Level { get; }

        double Latitude { get; }

        double Longitude { get; }

        ulong Updated { get; }
    }
}