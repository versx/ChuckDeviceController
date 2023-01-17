namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BaseStats
{
    [
        Column("date"),
        Key,
    ]
    public string Date { get; set; } = null!;

    [Column("count")]
    public ulong Count { get; set; }
}