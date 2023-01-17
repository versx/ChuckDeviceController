namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("invasion_stats")]
public class InvasionStats : BaseStats
{
    [
        Column("grunt_type"),
        Key,
    ]
    public ushort GruntType { get; set; }
}