namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BasePokemonStats
{
    [
        Key,
        Column("date"),
    ]
    public string Date { get; set; } = null!;

    [
        Key,
        Column("pokemon_id"),
    ]
    public uint PokemonId { get; set; }

    [
        Key,
        Column("form_id"),
    ]
    public ushort FormId { get; set; }

    [Column("count")]
    public ulong Count { get; set; }
}