namespace Chuck.Infrastructure.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    using Chuck.Infrastructure.Data.Interfaces;
    using Chuck.Infrastructure.Extensions;

    [Table("spawnpoint")]
    public class Spawnpoint : BaseEntity, IAggregateRoot
    {
        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
            JsonPropertyName("id"),
        ]
        public ulong Id { get; set; }

        [
            Column("lat"),
            JsonPropertyName("lat"),
        ]
        public double Latitude { get; set; }

        [
            Column("lon"),
            JsonPropertyName("lon"),
        ]
        public double Longitude { get; set; }

        [
            Column("despawn_sec"),
            JsonPropertyName("despawn_sec"),
        ]
        public ushort? DespawnSecond { get; set; }

        [
            Column("updated"),
            JsonPropertyName("updated"),
        ]
        public ulong Updated { get; set; }

        [
            Column("first_seen_timestamp"),
            JsonPropertyName("first_seen_timestamp"),
        ]
        public ulong FirstSeenTimestamp { get; set; }

        public static Spawnpoint FromPokemon(WildPokemonProto wild)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            var spawnpoint = new Spawnpoint
            {
                Id = Convert.ToUInt64(wild.SpawnPointId, 16),
                Latitude = wild.Latitude,
                Longitude = wild.Longitude,
                Updated = now,
                FirstSeenTimestamp = now, // TODO: Check if exists already before setting
            };
            var tthMs = wild.TimeTillHiddenMs;
            if (tthMs > 0 && tthMs <= 90000)
            {
                var unixDate = DateTime.UtcNow;
                var secondsOfHour = unixDate.Minute * 60 + unixDate.Second;
                var despawnSec = (int)Math.Round(tthMs / 1000.0);
                var offset = despawnSec < secondsOfHour
                    ? despawnSec - secondsOfHour
                    : 3600 - secondsOfHour + despawnSec;
                if (offset > 3600)
                {
                    offset -= 3600;
                }
                spawnpoint.DespawnSecond = (ushort)(secondsOfHour + offset);
            }
            return spawnpoint;
        }
    }
}