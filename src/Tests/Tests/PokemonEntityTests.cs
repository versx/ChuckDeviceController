namespace Tests;

using ChuckDeviceController.Data;
using ChuckDeviceController.Data.Entities;

internal class PokemonEntityTests
{
    [SetUp]
    public void Setup()
    {
    }

    //IEquatable, IComparable
    [Test]
    public void Test_Pokemon_Comparison_Pass()
    {
        var list = new List<Pokemon>
        {
            new Pokemon
            {
                Id = "1",
                PokemonId = 149,
            },
            new Pokemon
            {
                Id = "2",
                PokemonId = 151,
            },
            new Pokemon
            {
                Id = "5",
                PokemonId = 150,
            },
        };

        var pkmn = new Pokemon
        {
            Id = "5",
            PokemonId = 150,
        };
        var index = list.IndexOf(pkmn);
        var result = index == 2;
        Assert.That(result, Is.True);

        var contains = list.Contains(pkmn);
        Assert.That(contains, Is.True);
    }
}