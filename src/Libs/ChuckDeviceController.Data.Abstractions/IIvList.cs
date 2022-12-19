namespace ChuckDeviceController.Data.Abstractions;

public interface IIvList : IBaseEntity
{
    string Name { get; }

    List<string> PokemonIds { get; }
}