namespace ChuckDeviceController.Common.Abstractions;

public interface IIvList : IBaseEntity
{
    string Name { get; }

    List<string> PokemonIds { get; }
}