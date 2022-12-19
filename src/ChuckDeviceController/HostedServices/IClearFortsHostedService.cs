namespace ChuckDeviceController.HostedServices;

public interface IClearFortsHostedService
{
    void AddPokestop(ulong cellId, string pokestopId);

    void AddGym(ulong cellId, string gymId);

    void ClearPokestops();

    void ClearGyms();
}