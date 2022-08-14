namespace ChuckDeviceController.Services
{
    public interface IClearFortsService
    {
        void AddCell(ulong cellId);

        void AddPokestop(ulong cellId, string pokestopId);

        void AddGym(ulong cellId, string gymId);

        Task ClearOldFortsAsync();
    }
}