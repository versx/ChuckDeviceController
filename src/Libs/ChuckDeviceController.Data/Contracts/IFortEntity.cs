namespace ChuckDeviceController.Data.Contracts
{
    public interface IFortEntity
    {
        public bool IsEnabled { get; }

        public bool IsDeleted { get; }

        public ulong CellId { get; }

        // TODO: Add PowerUp columns and other shared properties between forts
    }
}