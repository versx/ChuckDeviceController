namespace ChuckDeviceController.Data.Entities
{
    public interface IFortEntity
    {
        public bool IsEnabled { get; set; }

        public bool IsDeleted { get; set; }

        public ulong CellId { get; set; }
    }
}