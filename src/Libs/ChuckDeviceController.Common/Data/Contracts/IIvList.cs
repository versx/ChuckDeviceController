namespace ChuckDeviceController.Common.Data.Contracts
{
    public interface IIvList : IBaseEntity
    {
        string Name { get; }

        IList<uint> PokemonIds { get; }
    }
}