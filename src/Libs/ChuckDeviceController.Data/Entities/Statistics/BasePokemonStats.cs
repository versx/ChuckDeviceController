namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BasePokemonStats : BaseStats
{
    [
        Column("pokemon_id"),
        Key,
    ]
    public uint PokemonId { get; set; }

    [
        Column("form_id"),
        Key,
    ]
    public ushort FormId { get; set; }
}