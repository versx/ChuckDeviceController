namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Extensions;

    [Table("spawnpoint")]
    public class Spawnpoint : BaseEntity
    {
        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public ulong Id { get; set; }

        [Column("lat")]
        public double Latitude { get; set; }

        [Column("lon")]
        public double Longitude { get; set; }

        [Column("despawn_sec")]
        public uint? DespawnSecond { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [Column("last_seen")]
        public ulong? LastSeen { get; set; }

        #endregion

        public Spawnpoint()
        {
        }

        public async Task UpdateAsync(MapDataContext context, bool update = false)
        {
            Spawnpoint? oldSpawnpoint = null;
            try
            {
                oldSpawnpoint = await context.Spawnpoints.FindAsync(Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Spawnpoint: {ex}");
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;
            LastSeen = now;

            if (!update && oldSpawnpoint != null)
            {
                return;
            }

            if (oldSpawnpoint != null)
            {
                if (DespawnSecond == null && oldSpawnpoint.DespawnSecond != null)
                {
                    DespawnSecond = oldSpawnpoint.DespawnSecond;
                }
                if (Latitude == oldSpawnpoint.Latitude &&
                    Longitude == oldSpawnpoint.Longitude &&
                    DespawnSecond == oldSpawnpoint.DespawnSecond)
                {
                    // TODO: Return false to update
                    return;
                }
            }

            Updated = now;
            LastSeen = now;
        }
    }
}