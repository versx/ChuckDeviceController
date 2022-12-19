namespace ChuckDeviceController.JobControllers.Models;

using ChuckDeviceController.Data.Entities;

internal class ScannedPokemon
{
    public Pokemon Pokemon { get; set; }

    public ulong DateScanned { get; set; }

    public ScannedPokemon(Pokemon pokemon, ulong dateScanned)
    {
        Pokemon = pokemon;
        DateScanned = dateScanned;
    }
}