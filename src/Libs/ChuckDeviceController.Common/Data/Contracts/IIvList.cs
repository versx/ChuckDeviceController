namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IIvList : IBaseEntity
    {
        string Name { get; }

        List<uint> PokemonIds { get; }
    }
}