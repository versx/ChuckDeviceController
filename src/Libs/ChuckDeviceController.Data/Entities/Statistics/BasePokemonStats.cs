namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class BasePokemonStats
    {
        [
            Column("date"),
            DataType(DataType.Date),
            DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true),
        ]
        public DateTime Date { get; set; }

        [Column("pokemon_id")]
        public uint PokemonId { get; set; }

        [Column("form_id")]
        public uint FormId { get; set; }

        [Column("count")]
        public ulong Count { get; set; }
    }
}