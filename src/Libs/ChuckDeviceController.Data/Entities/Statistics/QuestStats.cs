namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("quest_stats")]
public class QuestStats : BaseStats
{
    [
        Column("reward_type"),
        Key,
    ]
    public ushort RewardType { get; set; }

    [
        Column("pokemon_id"),
        Key,
    ]
    public uint PokemonId { get; set; }

    [
        Column("item_id"),
        Key,
    ]
    public ushort ItemId { get; set; }

    [
        Column("is_alternative"),
        Key,
    ]
    public bool IsAlternative { get; set; }
}