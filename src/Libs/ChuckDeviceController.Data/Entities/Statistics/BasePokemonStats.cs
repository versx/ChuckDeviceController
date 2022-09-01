namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class BasePokemonStats
    {
        [
            Key,
            Column("date"),
            DataType(DataType.Date),
            DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true),
        ]
        public DateTime Date { get; set; }

        [
            Key,
            Column("pokemon_id"),
        ]
        public uint PokemonId { get; set; }

        [
            Key,
            Column("form_id"),
        ]
        public uint FormId { get; set; }

        [Column("count")]
        public ulong Count { get; set; }
    }
}