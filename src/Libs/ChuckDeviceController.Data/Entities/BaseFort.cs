namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ChuckDeviceController.Common.Abstractions;

public class BaseFort : BaseEntity, ICoordinateEntity, IFortEntity
{
    [
        Column("id"),
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.None),
    ]
    public string Id { get; set; } = null!;

    [Column("lat")]
    public double Latitude { get; set; }

    [Column("lon")]
    public double Longitude { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("url")]
    public string? Url { get; set; }

    [
        DisplayName("Enabled"),
        Column("enabled"),
    ]
    public bool IsEnabled { get; set; }

    [
        DisplayName("Deleted"),
        Column("deleted"),
    ]
    public bool IsDeleted { get; set; }

    [
        Column("cell_id"),
        ForeignKey("cell_id"),
    ]
    public ulong CellId { get; set; }

    public virtual Cell? Cell { get; set; }

    [Column("power_up_points")]
    public uint? PowerUpPoints { get; set; }

    [Column("power_up_level")]
    public ushort? PowerUpLevel { get; set; }

    [Column("power_up_end_timestamp")]
    public ulong? PowerUpEndTimestamp { get; set; }

    [Column("first_seen_timestamp")]
    public ulong FirstSeenTimestamp { get; set; }

    [Column("last_modified_timestamp")]
    public ulong LastModifiedTimestamp { get; set; }

    [
        DisplayName("Last Updated"),
        Column("updated"),
    ]
    public ulong Updated { get; set; }
}