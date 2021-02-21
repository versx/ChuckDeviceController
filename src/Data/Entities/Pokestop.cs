namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Data.Interfaces;
    using ChuckDeviceController.Extensions;
    using ChuckDeviceController.Net.Webhooks;

    [Table("pokestop")]
    public class Pokestop : BaseEntity, IAggregateRoot, IWebhook
    {
        public const uint LureTime = 1800;

        [
            Column("id"),
            Key,
        ]
        public string Id { get; set; }

        [Column("lat")]
        public double Latitude { get; set; }

        [Column("lon")]
        public double Longitude { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("url")]
        public string Url { get; set; }

        [Column("lure_expire_timestamp")]
        public ulong? LureExpireTimestamp { get; set; }

        [Column("last_modified_timestamp")]
        public ulong LastModifiedTimestamp { get; set; }

        [Column("updated")]
        public ulong Updated { get; set; }

        [Column("enabled")]
        public bool Enabled { get; set; }

        [Column("quest_type")]
        public QuestType? QuestType { get; set; }

        [Column("quest_timestamp")]
        public ulong? QuestTimestamp { get; set; }

        [Column("quest_target")]
        public uint? QuestTarget { get; set; }

        [Column("quest_conditions")]
        public dynamic QuestConditions { get; set; } // TODO: QuestConditionProto

        [Column("quest_rewards")]
        public dynamic QuestRewards { get; set; } // TODO: QuestConditionProto

        [Column("quest_template")]
        public string QuestTemplate { get; set; }

        [Column("quest_pokemon_id")]
        public uint? QuestPokemonId { get; } // Virtual column

        [Column("quest_reward_type")]
        public uint? QuestRewardType { get; } // Virtual column

        [Column("quest_item_id")]
        public uint? QuestItemId { get; } // Virtual column

        [Column("cell_id")]
        public ulong CellId { get; set; }

        [Column("deleted")]
        public bool Deleted { get; set; }

        [Column("lure_id")]
        public uint LureId { get; set; }

        [Column("pokestop_display")]
        public uint? PokestopDisplay { get; set; }

        [Column("incident_expire_timestamp")]
        public ulong? IncidentExpireTimestamp { get; set; }

        [Column("first_seen_timestamp")]
        public ulong FirstSeenTimestamp { get; set; }

        [Column("grunt_type")]
        public uint? GruntType { get; set; }

        [Column("sponsor_id")]
        public uint? SponsorId { get; set; }

        [Column("ar_scan_eligible")]
        public bool IsArScanEligible { get; set; }

        [NotMapped]
        public bool HasChanges { get; set; }

        [NotMapped]
        public bool HasQuestChanges { get; set; }

        public Pokestop()
        {
        }

        public Pokestop(ulong cellId, PokemonFortProto fort)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            Id = fort.FortId;
            Latitude = fort.Latitude;
            Longitude = fort.Longitude;
            SponsorId = (uint)fort?.Sponsor;
            Enabled = fort.Enabled;
            LastModifiedTimestamp = (ulong)(fort.LastModifiedMs / 1000);
            CellId = cellId;
            FirstSeenTimestamp = now;
            Updated = now;
            Deleted = false;
            IsArScanEligible = fort.IsArScanEligible;
            if (fort.ActiveFortModifier?.Count > 0 &&
                (fort.ActiveFortModifier.Contains(Item.TroyDisk) ||
                 fort.ActiveFortModifier.Contains(Item.TroyDiskGlacial) ||
                 fort.ActiveFortModifier.Contains(Item.TroyDiskMagnetic) ||
                 fort.ActiveFortModifier.Contains(Item.TroyDiskMossy)))
            {
                LureExpireTimestamp = (ulong)Math.Floor(Convert.ToDouble(LastModifiedTimestamp + LureTime));
                LureId = (uint)fort.ActiveFortModifier.FirstOrDefault();
            }
            if (fort.PokestopDisplay != null)
            {
                IncidentExpireTimestamp = (ulong)Math.Floor(Convert.ToDouble(fort.PokestopDisplay.IncidentExpirationMs / 1000));
                if (fort.PokestopDisplay?.CharacterDisplay != null)
                {
                    PokestopDisplay = (uint?)fort.PokestopDisplay.CharacterDisplay?.Style;
                    GruntType = (uint)fort.PokestopDisplay.CharacterDisplay?.Character;
                }
            }
            else if (fort.PokestopDisplays?.Count > 0)
            {
                var pokestopDisplay = fort.PokestopDisplays.FirstOrDefault();
                IncidentExpireTimestamp = (ulong)Math.Floor(Convert.ToDouble(pokestopDisplay.IncidentExpirationMs / 1000));
                if (fort.PokestopDisplays.FirstOrDefault()?.CharacterDisplay != null)
                {
                    PokestopDisplay = (uint?)pokestopDisplay.CharacterDisplay.Style;
                    GruntType = (uint)pokestopDisplay.CharacterDisplay.Character;
                }
            }
        }

        public bool Update(Pokestop oldPokestop = null, bool updateQuest = false)
        {
            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;
            if (oldPokestop != null)
            {
                if (oldPokestop.CellId > 0 && CellId == 0)
                {
                    CellId = oldPokestop.CellId;
                }
                if (!string.IsNullOrEmpty(oldPokestop.Name) && string.IsNullOrEmpty(Name))
                {
                    Name = oldPokestop.Name;
                }
                if (!string.IsNullOrEmpty(oldPokestop.Url) && string.IsNullOrEmpty(Url))
                {
                    Url = oldPokestop.Url;
                }
                if (updateQuest && oldPokestop.QuestType != null && QuestType == null)
                {
                    QuestType = oldPokestop.QuestType;
                    QuestTarget = oldPokestop.QuestTarget;
                    QuestConditions = oldPokestop.QuestConditions;
                    QuestRewards = oldPokestop.QuestRewards;
                    QuestTimestamp = oldPokestop.QuestTimestamp;
                    QuestTemplate = oldPokestop.QuestTemplate;
                }
                if (oldPokestop.LureId > 0 && LureId == 0)
                {
                    LureId = oldPokestop.LureId;
                }
                if (!ShouldUpdate(oldPokestop, this))
                {
                    return false;
                }
                if ((oldPokestop.LureExpireTimestamp ?? 0) < (LureExpireTimestamp ?? 0))
                {
                    WebhookController.Instance.AddLure(this);
                    return true;
                }
                if ((oldPokestop.IncidentExpireTimestamp ?? 0) < (IncidentExpireTimestamp ?? 0))
                {
                    WebhookController.Instance.AddInvasion(this);
                    return true;
                }
                if (updateQuest && (QuestTimestamp ?? 0) > (oldPokestop.QuestTimestamp ?? 0))
                {
                    WebhookController.Instance.AddQuest(this);
                    return true;
                }
            }

            if (oldPokestop == null)
            {
                WebhookController.Instance.AddPokestop(this);
                return true;
                if (LureExpireTimestamp > 0)
                {
                    WebhookController.Instance.AddLure(this);
                    return true;
                }
                if (QuestTimestamp > 0)
                {
                    WebhookController.Instance.AddQuest(this);
                    return true;
                }
                if (IncidentExpireTimestamp > 0)
                {
                    WebhookController.Instance.AddInvasion(this);
                    return true;
                }
            }
            else
            {
                if (oldPokestop.LureExpireTimestamp < LureExpireTimestamp)
                {
                    WebhookController.Instance.AddLure(this);
                    return true;
                }
                if (oldPokestop.IncidentExpireTimestamp < IncidentExpireTimestamp)
                {
                    WebhookController.Instance.AddInvasion(this);
                    return true;
                }
                if (updateQuest && (HasQuestChanges || QuestTimestamp > oldPokestop.QuestTimestamp))
                {
                    HasQuestChanges = false;
                    WebhookController.Instance.AddQuest(this);
                    return true;
                }
            }
            return false;
        }

        public void AddDetails(FortDetailsOutProto fortDetails)
        {
            Id = fortDetails.Id;
            Latitude = fortDetails.Latitude;
            Longitude = fortDetails.Longitude;
            if (fortDetails.ImageUrl.Count > 0)
            {
                var url = fortDetails.ImageUrl.FirstOrDefault();
                // Check if url changed
                if (string.Compare(Url, url, true) != 0)
                {
                    HasChanges = true;
                    Url = url;
                }
            }
            var name = fortDetails.Name;
            if (string.Compare(Name, name, true) != 0)
            {
                HasChanges = true;
                Name = name;
            }
            Updated = DateTime.UtcNow.ToTotalSeconds();
        }

        public void AddQuest(QuestProto quest)
        {
            var conditions = new List<dynamic>();
            var rewards = new List<dynamic>();
            HasChanges = true;
            HasQuestChanges = true;
            foreach (var condition in quest.Goal.Condition)
            {
                var conditionData = new Dictionary<string, dynamic>();
                var infoData = new Dictionary<string, dynamic>();
                conditionData.Add("type", condition.Type);
                switch (condition.Type)
                {
                    case QuestConditionProto.Types.ConditionType.WithBadgeType:
                        infoData.Add("amount", condition.WithBadgeType.Amount);
                        infoData.Add("badge_rank", condition.WithBadgeType.BadgeRank);
                        var badgeTypesById = new List<uint>();
                        condition.WithBadgeType.BadgeType?.ToList()?.ForEach(x => badgeTypesById.Add((uint)x));
                        infoData.Add("badge_types", condition.WithBadgeType.BadgeRank);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithItem:
                        if (condition.WithItem.Item != Item.Unknown)
                        {
                            infoData.Add("item_id", condition.WithItem.Item);
                        }
                        break;
                    case QuestConditionProto.Types.ConditionType.WithRaidLevel:
                        var raidLevelsById = new List<ushort>();
                        condition.WithRaidLevel.RaidLevel?.ToList()?.ForEach(x => raidLevelsById.Add((ushort)x));
                        infoData.Add("raid_levels", raidLevelsById);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithPokemonType:
                        var pokemonTypesById = new List<HoloPokemonType>();
                        condition.WithPokemonType.PokemonType?.ToList()?.ForEach(x => pokemonTypesById.Add(x));
                        infoData.Add("pokemon_type_ids", pokemonTypesById);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithPokemonCategory:
                        if (!string.IsNullOrEmpty(condition.WithPokemonCategory.CategoryName))
                        {
                            infoData.Add("category_name", condition.WithPokemonCategory.CategoryName);
                        }
                        else
                        {
                            infoData.Add("pokemon_ids", condition.WithPokemonCategory.PokemonIds);
                        }
                        break;
                    case QuestConditionProto.Types.ConditionType.WithWinRaidStatus:
                        // TODO: QuestCondition.WithWinRaidStatus
                        break;
                    case QuestConditionProto.Types.ConditionType.WithThrowType:
                    case QuestConditionProto.Types.ConditionType.WithThrowTypeInARow:
                        if (condition.WithThrowType.ThrowType != HoloActivityType.ActivityUnknown)
                        {
                            infoData.Add("throw_type_id", (uint)condition.WithThrowType.ThrowType);
                        }
                        infoData.Add("hit", condition.WithThrowType.Hit);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithLocation:
                        infoData.Add("cell_ids", condition.WithLocation.S2CellId);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithDistance:
                        infoData.Add("distance", condition.WithDistance.DistanceKm);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithPokemonAlignment:
                        infoData.Add("alignment_ids", condition.WithPokemonAlignment.Alignment);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithInvasionCharacter:
                        infoData.Add("character_category_ids", condition.WithInvasionCharacter.Category);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithNpcCombat:
                        infoData.Add("win", condition.WithNpcCombat?.RequiresWin ?? false);
                        infoData.Add("template_ids", condition.WithNpcCombat?.CombatNpcTrainerId);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithPvpCombat:
                        infoData.Add("win", condition.WithPvpCombat?.RequiresWin ?? false);
                        infoData.Add("template_ids", condition.WithPvpCombat?.CombatLeagueTemplateId);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithBuddy:
                        if (condition.WithBuddy != null)
                        {
                            infoData.Add("min_buddy_level", condition.WithBuddy.MinBuddyLevel);
                            infoData.Add("must_be_on_map", condition.WithBuddy.MustBeOnMap);
                        }
                        break;
                    case QuestConditionProto.Types.ConditionType.WithDailyBuddyAffection:
                        infoData.Add("min_buddy_affection_earned_today", condition.WithDailyBuddyAffection.MinBuddyAffectionEarnedToday);
                        break;
                    case QuestConditionProto.Types.ConditionType.WithTempEvoPokemon:
                        infoData.Add("raid_pokemon_evolutions", condition.WithTempEvoId.MegaForm);
                        break;
                    default:
                        ConsoleExt.WriteWarn($"[Pokestop] Unrecognized condition type: {condition.Type}");
                        break;
                }
                conditionData.Add("info", infoData);
                conditions.Add(conditionData);
            }
            foreach (var reward in quest.QuestRewards)
            {
                var rewardData = new Dictionary<string, dynamic>();
                var infoData = new Dictionary<string, dynamic>();
                rewardData.Add("type", reward.Type);
                switch (reward.Type)
                {
                    case QuestRewardProto.Types.Type.Candy:
                        infoData.Add("amount", reward.Candy.Amount);
                        break;
                    case QuestRewardProto.Types.Type.Experience:
                        infoData.Add("amount", reward.Exp);
                        break;
                    case QuestRewardProto.Types.Type.Item:
                        infoData.Add("amount", reward.Item.Amount);
                        infoData.Add("item_id", reward.Item.Item);
                        break;
                    case QuestRewardProto.Types.Type.PokemonEncounter:
                        if (reward.PokemonEncounter.IsHiddenDitto)
                        {
                            infoData.Add("pokemon_id", Pokemon.DittoPokemonId);
                            infoData.Add("pokemon_id_display", reward.PokemonEncounter.PokemonId);
                        }
                        else
                        {
                            infoData.Add("pokemon_id", reward.PokemonEncounter.PokemonId);
                        }
                        if (reward.PokemonEncounter.PokemonDisplay != null)
                        {
                            infoData.Add("costume_id", reward.PokemonEncounter?.PokemonDisplay?.Costume ?? 0);
                            infoData.Add("form_id", reward.PokemonEncounter?.PokemonDisplay?.Form ?? 0);
                            infoData.Add("gender_id", reward.PokemonEncounter?.PokemonDisplay?.Gender ?? 0);
                            infoData.Add("shiny", reward.PokemonEncounter?.PokemonDisplay?.Shiny ?? false);
                        }
                        break;
                    case QuestRewardProto.Types.Type.Stardust:
                        infoData.Add("amount", reward.Stardust);
                        break;
                    case QuestRewardProto.Types.Type.MegaResource:
                        infoData.Add("amount", reward.MegaResource.Amount);
                        infoData.Add("pokemon_id", reward.MegaResource.PokemonId);
                        break;
                    default:
                        ConsoleExt.WriteWarn($"[Pokestop] Unrecognized reward type: {reward.Type}");
                        break;
                }
                rewardData.Add("info", infoData);
                rewards.Add(rewardData);
            }
            var now = DateTime.UtcNow.ToTotalSeconds();
            Id = quest.FortId;
            QuestType = quest.QuestType;
            QuestTarget = (uint?)quest.Goal.Target;
            QuestTemplate = quest.TemplateId.ToLower();
            QuestTimestamp = (ulong)Math.Floor(Convert.ToDouble(quest.LastUpdateTimestampMs / 1000));
            QuestConditions = conditions.Count > 0 ? conditions : null;
            QuestRewards = rewards.Count > 0 ? rewards : null;
            FirstSeenTimestamp = now;
            Updated = now;
            CellId = (ulong)quest.S2CellId;
            Deleted = false;
        }

        public dynamic GetWebhookValues(string type)
        {
            return (type.ToLower()) switch
            {
                "quest" => new
                {
                    type = "quest",
                    message = new
                    {
                        pokestop_id = Id,
                        latitude = Latitude,
                        longitude = Longitude,
                        type = QuestType,
                        target = QuestTarget,
                        template = QuestTemplate,
                        conditions = QuestConditions,
                        rewards = QuestRewards,
                        updated = QuestTimestamp,
                        pokestop_name = string.IsNullOrEmpty(Name) ? "Unknown" : Name,
                        pokestop_url = Url ?? "",
                    },
                },
                "invasion" => new
                {
                    type = "invasion",
                    message = new
                    {
                        pokestop_id = Id,
                        latitude = Latitude,
                        longitude = Longitude,
                        name = string.IsNullOrEmpty(Name) ? "Unknown" : Name,
                        url = Url ?? "",
                        lure_expiration = LureExpireTimestamp ?? 0,
                        last_modified = LastModifiedTimestamp,
                        enabled = Enabled,
                        lure_id = LureId,
                        pokestop_display = PokestopDisplay,
                        incident_expire_timestamp = IncidentExpireTimestamp,
                        grunt_type = GruntType,
                        updated = Updated,
                    },
                },
                _ => new
                {
                    type = "pokestop",
                    message = new
                    {
                        pokestop_id = Id,
                        latitude = Latitude,
                        longitude = Longitude,
                        name = string.IsNullOrEmpty(Name) ? "Unknown" : Name,
                        url = Url ?? "",
                        lure_expiration = LureExpireTimestamp ?? 0,
                        last_modified = LastModifiedTimestamp,
                        enabled = Enabled,
                        lure_id = LureId,
                        pokestop_display = PokestopDisplay,
                        incident_expire_timestamp = IncidentExpireTimestamp,
                        updated = Updated,
                    },
                },
            };
        }

        public static bool ShouldUpdate(Pokestop oldPokestop, Pokestop newPokestop)
        {
            if (oldPokestop.HasChanges)
            {
                oldPokestop.HasChanges = false;
                return true;
            }
            return
                oldPokestop.LastModifiedTimestamp != newPokestop.LastModifiedTimestamp ||
                oldPokestop.LureExpireTimestamp != newPokestop.LureExpireTimestamp ||
                oldPokestop.LureId != newPokestop.LureId ||
                oldPokestop.IncidentExpireTimestamp != newPokestop.IncidentExpireTimestamp ||
                oldPokestop.GruntType != newPokestop.GruntType ||
                oldPokestop.PokestopDisplay != newPokestop.PokestopDisplay ||
                oldPokestop.Name != newPokestop.Name ||
                oldPokestop.Url != newPokestop.Url ||
                oldPokestop.QuestTemplate != newPokestop.QuestTemplate ||
                oldPokestop.Enabled != newPokestop.Enabled ||
                oldPokestop.SponsorId != newPokestop.SponsorId ||
                Math.Abs(oldPokestop.Latitude - newPokestop.Latitude) >= 0.000001 ||
                Math.Abs(oldPokestop.Longitude - newPokestop.Longitude) >= 0.000001;
        }
    }
}