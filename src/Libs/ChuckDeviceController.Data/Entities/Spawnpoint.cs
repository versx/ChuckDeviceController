namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;

    [Table("spawnpoint")]
    public class Spawnpoint : BaseEntity, ISpawnpoint, ICoordinateEntity
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

        [NotMapped]
        public bool HasChanges { get; set; }

        //public virtual ICollection<Pokemon>? Pokemon { get; set; }

        #endregion

        #region Constructor

        public Spawnpoint()
        {
        }

        #endregion

        #region Public Methods

        public async Task UpdateAsync(MapDbContext context, IMemoryCacheHostedService memCache, bool update = false)
        {
            Spawnpoint? oldSpawnpoint = null;
            try
            {
                // Check cache first for spawnpoint entity
                var cached = memCache.Get<ulong, Spawnpoint>(Id);
                if (cached != null)
                {
                    oldSpawnpoint = cached;
                }
                else
                {
                    oldSpawnpoint = await context.Spawnpoints.FindAsync(Id);
                    if (oldSpawnpoint != null)
                    {
                        memCache.Set(Id, oldSpawnpoint);
                    }
                    else
                    {
                        memCache.Set(Id, this);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Spawnpoint: {ex}");
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;
            LastSeen = now;

            if (update && oldSpawnpoint != null)
            {
                if (DespawnSecond == null && oldSpawnpoint.DespawnSecond != null)
                {
                    DespawnSecond = oldSpawnpoint.DespawnSecond;
                    HasChanges = true;
                }
                if (Latitude == oldSpawnpoint.Latitude &&
                    Longitude == oldSpawnpoint.Longitude &&
                    DespawnSecond == oldSpawnpoint.DespawnSecond)
                {
                    // No changes between current and old spawnpoints
                    return;
                }
            }

            // Cache spawnpoint entity by id
            memCache.Set(Id, this);
        }

        #endregion
    }
}