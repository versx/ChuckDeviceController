namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using Chuck.Infrastructure.Data.Interfaces;

    [Table("iv_list")]
    public class IVList : BaseEntity, IAggregateRoot
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
        public List<uint> PokemonIDs { get; set; }
    }
}