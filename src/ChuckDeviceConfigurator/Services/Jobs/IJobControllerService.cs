namespace ChuckDeviceConfigurator.Services.Jobs
{
    using POGOProtos.Rpc;

    using ChuckDeviceController.Data.Entities;

    public interface IJobControllerService
    {
        void Start();

        void Stop();

        #region Instances

        IJobController GetInstanceController(string uuid);

        Task<string> GetStatusAsync(Instance instance);

        Task AddInstanceAsync(Instance instance);

        Task ReloadInstanceAsync(Instance newInstance, string oldInstanceName);

        void ReloadAllInstances();

        Task RemoveInstanceAsync(string instanceName);

        IReadOnlyList<Pokemon> GetIvQueue(string name);

        void RemoveFromIvQueue(string name, string encounterId);

        #endregion

        #region Devices

        void AddDevice(Device device);

        Task RemoveDeviceAsync(Device device);

        void RemoveDevice(string uuid);

        void ReloadDevice(Device device, string oldDeviceUuid);

        List<string> GetDeviceUuidsInInstance(string instanceName);

        #endregion

        void GotPokemon(Pokemon pokemon);

        void GotPokemonIV(Pokemon pokemon);

        void GotFort(PokemonFortProto fort, string username);

        void GotPlayerData(string username, ushort level, ulong xp);
    }
}