namespace ChuckDeviceController.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using MySqlConnector;

using ChuckDeviceController.Caching.Memory;
using ChuckDeviceController.Common.Abstractions;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Extensions;

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

    [JsonIgnore]
    public virtual ICollection<Pokemon>? Pokemon { get; set; }

    #endregion

    #region Constructor

    public Spawnpoint()
    {
    }

    #endregion

    #region Public Methods

    public async Task UpdateAsync(Spawnpoint? oldSpawnpoint, IMemoryCacheService memCache, bool update = false, bool isTimestampAccurate = true)
    {
        if (!update && oldSpawnpoint != null)
        {
            return;
        }

        var now = DateTime.UtcNow.ToTotalSeconds();
        Updated = now;

        if (EntityConfiguration.Instance.SaveSpawnpointLastSeen)
        {
            LastSeen = now;
        }

        if (oldSpawnpoint == null)
        {
            HasChanges = true;
        }
        else
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

            HasChanges = true;
        }

        if (!HasChanges)
            return;

        // Better to have an inaccurate timestamp than none at all -> only update if the
        // time differs more than 3 minutes, then use the old despawn seconds value.
        // Otherwise keep new despawn seconds value.
        if (!isTimestampAccurate && oldSpawnpoint?.DespawnSecond != null)
        {
            var oldDespawnSecond = oldSpawnpoint.DespawnSecond ?? 0;
            var newDespawnSecond = DespawnSecond;

            // Depending on the other is great than the other, we have to
            // subtract from the smaller value to get a valid result.
            var absDiff = Math.Abs(Convert.ToDecimal(oldDespawnSecond - newDespawnSecond));
            var secondAbsDifference = 3600 - absDiff;

            // Difference can be either 900 or 2700 - e.g. if you compare DespawnSecond 800 with 3500.
            if (absDiff < secondAbsDifference && absDiff < 180)
            {
                DespawnSecond = oldDespawnSecond;
            }
            else if (absDiff > secondAbsDifference && secondAbsDifference < 180)
            {
                DespawnSecond = oldDespawnSecond;
            }
        }

        // Cache spawnpoint entity by id
        memCache.Set(Id, this);

        await Task.CompletedTask;
    }

    #endregion
}