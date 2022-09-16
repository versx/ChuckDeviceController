namespace ChuckDeviceController.HostedServices
{
    public interface IClearFortsHostedService
    {
        void AddCell(ulong cellId);

        void AddPokestop(ulong cellId, string pokestopId);

        void AddGym(ulong cellId, string gymId);

        Task ClearOldFortsAsync();
    }
}