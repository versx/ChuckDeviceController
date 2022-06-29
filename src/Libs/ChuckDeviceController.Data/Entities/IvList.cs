namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    [Table("iv_list")]
    public class IvList : BaseEntity
    {
        [
            Column("name"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("name"),
        ]
        public string Name { get; set; }

        [
            Column("pokemon_ids"),
            JsonPropertyName("pokemon_ids"),
        ]
        public List<uint> PokemonIds { get; set; }
    }

    /*
    public class IvListData
    {
        [JsonPropertyName("pokemon_ids")]
        public List<uint> PokemonIds { get; set; }
    }
    */
}