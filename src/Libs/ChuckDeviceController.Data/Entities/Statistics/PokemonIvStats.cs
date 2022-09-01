namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Microsoft.EntityFrameworkCore;

    [Table("pokemon_iv_stats")]
    public class PokemonIvStats : BasePokemonStats
    {
        [
            Key,
            Column("iv"),
            Precision(3, 2),
        ]
        public double IV { get; set; }
    }
}