namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations.Schema;

[Table("raid_stats")]
public class RaidStats : BasePokemonStats
{
    [Column("level")]
    public ushort Level { get; set; }
}