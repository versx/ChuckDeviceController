namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using ChuckDeviceController.Data.Abstractions;

[Table("iv_list")]
public class IvList : BaseEntity, IIvList
{
    [
        DisplayName("Name"),
        Column("name"),
        Key,
        DatabaseGenerated(DatabaseGeneratedOption.None),
        JsonPropertyName("name"),
    ]
    public string Name { get; set; } = null!;

    [
        DisplayName("Pokemon"),
        Column("pokemon_ids"),
        JsonPropertyName("pokemon_ids"),
    ]
    public List<string> PokemonIds { get; set; } = new();
}

/*
public class IvListData
{
    [JsonPropertyName("pokemon_ids")]
    public List<uint> PokemonIds { get; set; }
}
*/