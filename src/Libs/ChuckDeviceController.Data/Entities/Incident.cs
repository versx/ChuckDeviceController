namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Extensions;

    [Table("incident")]
    public class Incident : BaseEntity, IWebhookPayload
    {
        public const string UnknownPokestopName = "Unknown";

        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public string Id { get; set; }

        [Column("pokestop_id")]
        public string PokestopId { get; set; }

        [Column("start")]
        public ulong Start { get; set; }

        [Column("expiration")]
        public ulong Expiration { get; set; }

        [Column("display_type")]
        public uint DisplayType { get; set; }

        [Column("style")]
        public uint Style { get; set; }

        [Column("character")]
        public uint Character { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [NotMapped]
        public bool HasChanges { get; set; }

        #endregion

        #region Constructor

        public Incident()
        {
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

        // TODO: Instead of relying on SendWebhook property, possibly have UpdateAsync return list/dict of webhook payloads
        // This would be especially needed for Gyms/Pokestops that could return multiple webhook types
        // Maybe return just the WebhookHeaders for the entity and compose the payload in the DataProcessorService <- sounds good/much better
        public async Task UpdateAsync(MapDataContext context)
        {
            Incident? oldIncident = null;
            try
            {
                oldIncident = await context.Incidents.FindAsync(Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pokestop: {ex}");
            }

            if (oldIncident != null)
            {
                // TODO: shouldUpdate
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;

            if (oldIncident == null)
            {
                Pokestop? pokestop = null;
                try
                {
                    pokestop = await context.Pokestops.FindAsync(PokestopId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Incident: {ex}");
                }

                if (pokestop != null)
                {
                    // TODO: Webhook
                }
            }
            else
            {
                if (oldIncident.Expiration < Expiration || oldIncident.Character != Character)
                {
                    Pokestop? pokestop = null;
                    try
                    {
                        pokestop = await context.Pokestops.FindAsync(PokestopId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Incident: {ex}");
                    }

                    if (pokestop != null)
                    {
                        // TODO: Webhook
                    }
                }
            }
        }

        public dynamic GetWebhookData(string type)
        {
            throw new NotImplementedException();
        }

        public dynamic GetWebhookData(string type, Pokestop pokestop)
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