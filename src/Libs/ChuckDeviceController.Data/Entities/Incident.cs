﻿namespace ChuckDeviceController.Data.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Data.Repositories;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Extensions.Http.Caching;

    [Table("incident")]
    public class Incident : BaseEntity, IIncident, IWebhookEntity
    {
        public const string UnknownPokestopName = "Unknown";

        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public string Id { get; set; }

        [
            Column("pokestop_id"),
            //ForeignKey(nameof(Pokestop)),
            ForeignKey("pokestop_id"),
            //ForeignKey("FK_incident_pokestop_pokestop_id"),
        ]
        public string PokestopId { get; set; }

        public virtual Pokestop? Pokestop { get; set; }

        [Column("start")]
        public ulong Start { get; set; }

        [Column("expiration")]
        public ulong Expiration { get; set; }

        [Column("display_type")]
        public uint DisplayType { get; set; }

        [Column("style")]
        public uint Style { get; set; }

        [Column("character")]
        public ushort Character { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [NotMapped]
        public bool HasChanges { get; set; }

        [NotMapped]
        public bool SendWebhook { get; set; }

        #endregion

        #region Constructor

        public Incident()
        {
            Id = string.Empty;
        }

        public Incident(ulong now, string pokestopId, PokestopIncidentDisplayProto pokestopDisplay)
        {
            Id = pokestopDisplay.IncidentId;
            PokestopId = pokestopId;
            Start = Convert.ToUInt64(pokestopDisplay.IncidentStartMs / 1000);
            Expiration = Convert.ToUInt64(pokestopDisplay.IncidentExpirationMs / 1000);
            DisplayType = Convert.ToUInt16(pokestopDisplay.IncidentDisplayType);
            Style = Convert.ToUInt16(pokestopDisplay.CharacterDisplay.Style);
            Character = Convert.ToUInt16(pokestopDisplay.CharacterDisplay.Character);
            Updated = now;
        }

        #endregion

        #region Public Methods

        public async Task UpdateAsync(MapDbContext context, IMemoryCacheHostedService memCache)
        {
            var oldIncident = await EntityRepository.GetEntityAsync<string, Incident, MapDbContext>(context, memCache, Id);
            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;

            if (oldIncident == null)
            {
                SendWebhook = true;
            }
            else
            {
                if (oldIncident.Expiration < Expiration || oldIncident.Character != Character)
                {
                    SendWebhook = true;
                }
            }

            // Cache pokestop incident entity by id
            memCache.Set(Id, this);
        }

        public dynamic? GetWebhookData(string type)
        {
            throw new NotImplementedException();
        }

        public dynamic? GetWebhookData(string type, Pokestop pokestop)
        {
            switch (type.ToLower())
            {
                case "invasion":
                    return new
                    {
                        type = WebhookHeaders.Invasion,
                        message = new
                        {
                            id = Id,
                            pokestop_id = PokestopId,
                            latitude = pokestop.Latitude,
                            longitude = pokestop.Longitude,
                            pokestop_name = pokestop.Name ?? UnknownPokestopName,
                            url = pokestop.Url ?? "",
                            enabled = pokestop.IsEnabled,
                            start = Start,
                            expiration = Expiration,
                            incident_expire_timestamp = Expiration, // deprecated: replaced with Incident.Expiration
                            display_type = DisplayType,
                            style = Style,
                            grunt_type = Character, // deprecated: replaced with Incident.Character
                            character = Character,
                            updated = Updated,
                        },
                    };
            }

            Console.WriteLine($"Received unknown invasion webhook payload type: {type}, returning null");
            return null;
        }

        #endregion
    }
}