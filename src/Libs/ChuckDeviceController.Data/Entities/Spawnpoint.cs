namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using MySqlConnector;

    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Data.Repositories;
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

        [Column("last_seen")]
        public ulong? LastSeen { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

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

        public async Task UpdateAsync(MySqlConnection connection, IMemoryCacheHostedService memCache, bool update = false, bool skipOldLookup = false)
        {
            var oldSpawnpoint = skipOldLookup
                ? null
                : await EntityRepository.GetEntityAsync<ulong, Spawnpoint>(connection, Id, memCache);
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

            await Task.CompletedTask;
        }

        #endregion
    }
}