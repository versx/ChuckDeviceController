namespace ChuckDeviceController.Data.Entities
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using POGOProtos.Rpc;
    using ConditionType = POGOProtos.Rpc.QuestConditionProto.Types.ConditionType;
    using RewardType = POGOProtos.Rpc.QuestRewardProto.Types.Type;

    using ChuckDeviceController.Common;
    using ChuckDeviceController.Common.Data;
    using ChuckDeviceController.Common.Data.Contracts;
    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Contracts;
    using ChuckDeviceController.Data.Extensions;
    using ChuckDeviceController.Extensions;

    [Table("pokestop")]
    public class Pokestop : BaseEntity, IPokestop, ICoordinateEntity, IFortEntity, IWebhookEntity
    {
        #region Constants

        public const ushort DefaultLureTimeS = 1800; // TODO: Make 'DefaultLureTimeS' configurable
        public const string UnknownPokestopName = "Unknown";

        #endregion

        #region Properties

        [
            Column("id"),
            Key,
            DatabaseGenerated(DatabaseGeneratedOption.None),
        ]
        public string Id { get; set; }

        [Column("lat")]
        public double Latitude { get; set; }

        [Column("lon")]
        public double Longitude { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("url")]
        public string? Url { get; set; }

        [Column("lure_id")]
        public uint LureId { get; set; }

        [Column("lure_expire_timestamp")]
        public ulong? LureExpireTimestamp { get; set; }

        [Column("last_modified_timestamp")]
        public ulong LastModifiedTimestamp { get; set; }

        [
            DisplayName("Last Updated"),
            Column("updated"),
        ]
        public ulong Updated { get; set; }

        [
            DisplayName("Enabled"),
            Column("enabled"),
        ]
        public bool IsEnabled { get; set; }

        [Column("cell_id")]
        public ulong CellId { get; set; }

        [
            DisplayName("Deleted"),
            Column("deleted"),
        ]
        public bool IsDeleted { get; set; }

        [Column("first_seen_timestamp")]
        public ulong FirstSeenTimestamp { get; set; }

        [Column("sponsor_id")]
        public uint? SponsorId { get; set; }

        [Column("ar_scan_eligible")]
        public bool IsArScanEligible { get; set; }

        [Column("power_up_points")]
        public uint? PowerUpPoints { get; set; }

        [Column("power_up_level")]
        public ushort? PowerUpLevel { get; set; }

        [Column("power_up_end_timestamp")]
        public ulong? PowerUpEndTimestamp { get; set; }

        #region Quests

        [Column("quest_type")]
        public uint? QuestType { get; set; }

        [Column("quest_template")]
        public string? QuestTemplate { get; set; }

        [Column("quest_title")]
        public string? QuestTitle { get; set; }

        [Column("quest_target")]
        public ushort? QuestTarget { get; set; }

        [Column("quest_timestamp")]
        public ulong? QuestTimestamp { get; set; }

        #region Virtual Columns

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("quest_reward_type"),
        ]
        public ushort? QuestRewardType { get; set; }

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("quest_item_id"),
        ]
        public ushort? QuestItemId { get; set; }

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("quest_reward_amount"),
        ]
        public ushort? QuestRewardAmount { get; set; }

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("quest_pokemon_id"),
        ]
        public uint? QuestPokemonId { get; set; }

        #endregion

        [Column("quest_conditions")]
        public List<Dictionary<string, dynamic>>? QuestConditions { get; set; }

        [Column("quest_rewards")]
        public List<Dictionary<string, dynamic>>? QuestRewards { get; set; }

        [Column("alternative_quest_type")]
        public uint? AlternativeQuestType { get; set; }

        [Column("alternative_quest_template")]
        public string? AlternativeQuestTemplate { get; set; }

        [Column("alternative_quest_title")]
        public string? AlternativeQuestTitle { get; set; }

        [Column("alternative_quest_target")]
        public ushort? AlternativeQuestTarget { get; set; }

        [Column("alternative_quest_timestamp")]
        public ulong? AlternativeQuestTimestamp { get; set; }

        #region Virtual Columns

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("alternative_quest_reward_type"),
        ]
        public ushort? AlternativeQuestRewardType { get; set; }

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("alternative_quest_item_id"),
        ]
        public ushort? AlternativeQuestItemId { get; set; }

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("alternative_quest_reward_amount"),
        ]
        public ushort? AlternativeQuestRewardAmount { get; set; }

        [
            DatabaseGenerated(DatabaseGeneratedOption.Computed),
            Column("alternative_quest_pokemon_id"),
        ]
        public uint? AlternativeQuestPokemonId { get; set; }

        #endregion

        [Column("alternative_quest_conditions")]
        public List<Dictionary<string, dynamic>>? AlternativeQuestConditions { get; set; }

        [Column("alternative_quest_rewards")]
        public List<Dictionary<string, dynamic>>? AlternativeQuestRewards { get; set; }

        #endregion

        [Column("incidents")]
        public ICollection<Incident>? Incidents { get; set; }

        [NotMapped]
        public bool HasChanges { get; set; }

        [NotMapped]
        public bool HasQuestChanges { get; set; }

        [NotMapped]
        public bool HasAlternativeQuestChanges { get; set; }

        #endregion

        #region Constructors

        public Pokestop()
        {
            Id = string.Empty;
        }

        public Pokestop(PokemonFortProto fortData, ulong s2cellId)
        {
            Id = fortData.FortId;
            Latitude = fortData.Latitude;
            Longitude = fortData.Longitude;
            //PartnerId = fortData.PartnerId != "" ? fortData.PartnerId : null;
            if (fortData.Sponsor != FortSponsor.Types.Sponsor.Unset)
            {
                SponsorId = Convert.ToUInt16(fortData.Sponsor);
            }
            IsEnabled = fortData.Enabled;
            IsArScanEligible = fortData.IsArScanEligible;

            var fortPowerLevel = fortData.GetFortPowerLevel();
            PowerUpPoints = fortPowerLevel.PowerUpPoints;
            PowerUpLevel = fortPowerLevel.PowerUpLevel;
            PowerUpEndTimestamp = fortPowerLevel.PowerUpEndTimestamp;

            var lastModifiedTimestamp = Convert.ToUInt64(fortData.LastModifiedMs / 1000);
            if (fortData.ActiveFortModifier != null)
            {
                if (fortData.ActiveFortModifier.Contains(Item.TroyDisk) ||
                    fortData.ActiveFortModifier.Contains(Item.TroyDiskGlacial) ||
                    fortData.ActiveFortModifier.Contains(Item.TroyDiskMossy) ||
                    fortData.ActiveFortModifier.Contains(Item.TroyDiskMagnetic) ||
                    fortData.ActiveFortModifier.Contains(Item.TroyDiskRainy))
                {
                    LureExpireTimestamp = lastModifiedTimestamp + DefaultLureTimeS;
                    LureId = Convert.ToUInt32(fortData.ActiveFortModifier[0]);
                }
            }
            LastModifiedTimestamp = lastModifiedTimestamp;
            if (!string.IsNullOrEmpty(fortData.ImageUrl))
            {
                Url = fortData.ImageUrl;
            }
            CellId = s2cellId;

            var incidents = fortData.PokestopDisplays?.ToList();
            if ((incidents?.Count ?? 0) == 0 && fortData.PokestopDisplay != null)
            {
                incidents = new List<PokestopIncidentDisplayProto>
                {
                    fortData.PokestopDisplay,
                };
            }

            if (incidents != null)
            {
                var now = DateTime.UtcNow.ToTotalSeconds();
                Incidents = incidents.Select(pokestopDisplay => new Incident(now, Id, pokestopDisplay))
                                     .ToList();
            }
        }

        #endregion

        #region Public Methods

        public void AddDetails(FortDetailsOutProto fortData)
        {
            Id = fortData.Id;
            Latitude = fortData.Latitude;
            Longitude = fortData.Longitude;
            if ((fortData.ImageUrl?.Count ?? 0) > 0)
            {
                var url = fortData.ImageUrl?.FirstOrDefault();
                if (Url != url)
                {
                    Url = url;
                    HasChanges = true;
                }
            }
            var name = fortData.Name;
            if (Name != name)
            {
                Name = name;
                HasChanges = true;
            }
        }

        public void AddQuest(string title, QuestProto questData, bool hasArQuest)
        {
            var questType = questData.QuestType;
            var questTarget = questData.Goal.Target;
            var questTemplate = questData.TemplateId.ToLower();
            var questTitle = title.ToLower();
            HasChanges = true;

            var conditions = new List<Dictionary<string, dynamic>>();
            var rewards = new List<Dictionary<string, dynamic>>();

            foreach (var conditionData in questData.Goal.Condition)
            {
                var condition = new Dictionary<string, dynamic>();
                var infoData = new Dictionary<string, dynamic>();
                condition.Add("type", conditionData.Type);

                switch (conditionData.Type)
                {
                    case ConditionType.WithBadgeType:
                        infoData.Add("amount", conditionData.WithBadgeType.Amount);
                        infoData.Add("badge_rank", conditionData.WithBadgeType.BadgeRank);
                        var badgeTypesById = new List<uint>();
                        foreach (var badge in conditionData.WithBadgeType.BadgeType)
                        {
                            badgeTypesById.Add(Convert.ToUInt32(badge));
                        }
                        infoData.Add("badge_types", badgeTypesById);
                        break;
                    case ConditionType.WithItem:
                        if (conditionData.WithItem.Item != Item.Unknown)
                        {
                            infoData.Add("item_id", conditionData.WithItem.Item);
                        }
                        break;
                    case ConditionType.WithRaidLevel:
                        var raidLevels = new List<ushort>();
                        foreach (var raidLevel in conditionData.WithRaidLevel.RaidLevel)
                        {
                            raidLevels.Add(Convert.ToUInt16(raidLevel));
                        }
                        infoData.Add("raid_levels", raidLevels);
                        break;
                    case ConditionType.WithPokemonType:
                        var pokemonTypes = new List<ushort>();
                        foreach (var type in conditionData.WithPokemonType.PokemonType)
                        {
                            pokemonTypes.Add(Convert.ToUInt16(type));
                        }
                        infoData.Add("pokemon_type_ids", pokemonTypes);
                        break;
                    case ConditionType.WithPokemonCategory:
                        if (!string.IsNullOrEmpty(conditionData.WithPokemonCategory.CategoryName))
                        {
                            infoData.Add("category_name", conditionData.WithPokemonCategory.CategoryName);
                        }
                        var pokemonById = new List<uint>();
                        foreach (var pokemon in conditionData.WithPokemonCategory.PokemonIds)
                        {
                            pokemonById.Add(Convert.ToUInt32(pokemon));
                        }
                        infoData.Add("pokemon_ids", pokemonById);
                        break;
                    case ConditionType.WithWinRaidStatus:
                        break;
                    case ConditionType.WithThrowType:
                    case ConditionType.WithThrowTypeInARow:
                        if (conditionData.WithThrowType != null && conditionData.WithThrowType.ThrowType != HoloActivityType.ActivityUnknown)
                        {
                            infoData.Add("throw_type_id", Convert.ToUInt32(conditionData.WithThrowType.ThrowType));
                            infoData.Add("hit", conditionData.WithThrowType.Hit);
                        }
                        break;
                    case ConditionType.WithLocation:
                        infoData.Add("cell_ids", conditionData.WithLocation.S2CellId);
                        break;
                    case ConditionType.WithDistance:
                        infoData.Add("distance", conditionData.WithDistance.DistanceKm);
                        break;
                    case ConditionType.WithPokemonAlignment:
                        infoData.Add("alignment_ids", conditionData.WithPokemonAlignment.Alignment.Select(x => Convert.ToUInt32(x)));
                        break;
                    case ConditionType.WithInvasionCharacter:
                        infoData.Add("character_category_ids", conditionData.WithInvasionCharacter.Category.Select(x => Convert.ToUInt32(x)));
                        break;
                    case ConditionType.WithNpcCombat:
                        infoData.Add("win", conditionData.WithNpcCombat.RequiresWin);
                        infoData.Add("trainer_ids", conditionData.WithNpcCombat.CombatNpcTrainerId);
                        break;
                    case ConditionType.WithPvpCombat:
                        infoData.Add("win", conditionData.WithPvpCombat.RequiresWin);
                        infoData.Add("template_ids", conditionData.WithPvpCombat.CombatLeagueTemplateId);
                        break;
                    case ConditionType.WithPlayerLevel:
                        infoData.Add("level", conditionData.WithPlayerLevel.Level);
                        break;
                    case ConditionType.WithBuddy:
                        if (conditionData.WithBuddy != null)
                        {
                            infoData.Add("min_buddy_level", Convert.ToUInt32(conditionData.WithBuddy.MinBuddyLevel));
                            infoData.Add("must_be_on_map", conditionData.WithBuddy.MustBeOnMap);
                        }
                        break;
                    case ConditionType.WithDailyBuddyAffection:
                        infoData.Add("min_buddy_affection_earned_today", conditionData.WithDailyBuddyAffection.MinBuddyAffectionEarnedToday);
                        break;
                    case ConditionType.WithTempEvoPokemon:
                        infoData.Add("raid_pokemon_evolutions", conditionData.WithTempEvoId.MegaForm.Select(x => Convert.ToUInt32(x)));
                        break;
                    case ConditionType.WithItemType:
                        infoData.Add("item_type_ids", conditionData.WithItemType.ItemType.Select(x => Convert.ToUInt32(x)));
                        break;
                    case ConditionType.WithRaidElapsedTime:
                        infoData.Add("time", Convert.ToUInt64(conditionData.WithElapsedTime.ElapsedTimeMs / 1000));
                        break;
                    case ConditionType.WithWinGymBattleStatus:
                    case ConditionType.WithSuperEffectiveCharge:
                    case ConditionType.WithUniquePokestop:
                    case ConditionType.WithQuestContext:
                    case ConditionType.WithWinBattleStatus:
                    case ConditionType.WithCurveBall:
                    case ConditionType.WithNewFriend:
                    case ConditionType.WithDaysInARow:
                    case ConditionType.WithWeatherBoost:
                    case ConditionType.WithDailyCaptureBonus:
                    case ConditionType.WithDailySpinBonus:
                    case ConditionType.WithUniquePokemon:
                    case ConditionType.WithUniquePokemonTeam:
                    case ConditionType.WithBuddyInterestingPoi:
                    case ConditionType.WithPokemonLevel:
                    case ConditionType.WithSingleDay:
                    case ConditionType.WithMaxCp:
                    case ConditionType.WithLuckyPokemon:
                    case ConditionType.WithLegendaryPokemon:
                    case ConditionType.WithGblRank:
                    case ConditionType.WithCatchesInARow:
                    case ConditionType.WithEncounterType:
                    case ConditionType.WithCombatType:
                    case ConditionType.WithGeotargetedPoi:
                    case ConditionType.WithFriendLevel:
                    case ConditionType.WithSticker:
                    case ConditionType.WithPokemonCp:
                    case ConditionType.WithRaidLocation:
                    case ConditionType.WithFriendsRaid:
                    case ConditionType.WithPokemonCostume:
                    case ConditionType.Unset:
                        break;
                }

                if (infoData.Count > 0)
                {
                    condition.Add("info", infoData);
                }
                conditions.Add(condition);
            }

            foreach (var rewardData in questData.QuestRewards)
            {
                var reward = new Dictionary<string, dynamic>();
                var infoData = new Dictionary<string, dynamic>();
                reward.Add("type", rewardData.Type);

                switch (rewardData.Type)
                {
                    case RewardType.Experience:
                        infoData.Add("amount", rewardData.Exp);
                        break;
                    case RewardType.Item:
                        infoData.Add("amount", rewardData.Item.Amount);
                        infoData.Add("item_id", Convert.ToUInt32(rewardData.Item.Item));
                        break;
                    case RewardType.Stardust:
                        infoData.Add("amount", rewardData.Stardust);
                        break;
                    case RewardType.Candy:
                        infoData.Add("amount", rewardData.Candy.Amount);
                        infoData.Add("pokemon_id", Convert.ToUInt32(rewardData.Candy.PokemonId));
                        break;
                    case RewardType.XlCandy:
                        infoData.Add("amount", rewardData.XlCandy.Amount);
                        infoData.Add("pokemon_id", Convert.ToUInt32(rewardData.XlCandy.PokemonId));
                        break;
                    case RewardType.PokemonEncounter:
                        var pokemonId = Convert.ToUInt32(rewardData.PokemonEncounter.PokemonId);
                        if (rewardData.PokemonEncounter.IsHiddenDitto)
                        {
                            infoData.Add("pokemon_id", Pokemon.DittoPokemonId);
                            infoData.Add("pokemon_id_display", pokemonId);
                        }
                        else
                        {
                            infoData.Add("pokemon_id", pokemonId);
                        }
                        if (rewardData.PokemonEncounter.PokemonDisplay != null)
                        {
                            var costume = Convert.ToUInt32(rewardData.PokemonEncounter.PokemonDisplay.Costume);
                            var form = Convert.ToUInt32(rewardData.PokemonEncounter.PokemonDisplay.Form);
                            var gender = Convert.ToUInt32(rewardData.PokemonEncounter.PokemonDisplay.Gender);
                            var shiny = rewardData.PokemonEncounter.PokemonDisplay.Shiny;
                            infoData.Add("costume_id", costume);
                            infoData.Add("form_id", form);
                            infoData.Add("gender_id", gender);
                            infoData.Add("shiny", shiny);
                        }
                        break;
                    case RewardType.Pokecoin:
                        infoData.Add("amount", rewardData.Pokecoin);
                        break;
                    case RewardType.Sticker:
                        infoData.Add("amount", rewardData.Sticker.Amount);
                        infoData.Add("sticker_id", rewardData.Sticker.StickerId);
                        break;
                    case RewardType.MegaResource:
                        infoData.Add("amount", rewardData.MegaResource.Amount);
                        infoData.Add("pokemon_id", Convert.ToUInt32(rewardData.MegaResource.PokemonId));
                        break;
                    case RewardType.AvatarClothing:
                    case RewardType.Quest:
                    case RewardType.LevelCap:
                    case RewardType.Incident:
                    case RewardType.PlayerAttribute:
                    case RewardType.Unset:
                    default:
                        break;
                }

                reward.Add("info", infoData);
                rewards.Add(reward);
            }

            var questConditions = conditions;
            var questRewards = rewards;
            var questTimestamp = DateTime.UtcNow.ToTotalSeconds();

            if (hasArQuest)
            {
                AlternativeQuestType = Convert.ToUInt32(questType);
                AlternativeQuestTarget = Convert.ToUInt16(questTarget);
                AlternativeQuestTemplate = questTemplate;
                AlternativeQuestTitle = questTitle;
                AlternativeQuestConditions = questConditions;
                AlternativeQuestRewards = questRewards;
                AlternativeQuestTimestamp = questTimestamp;
                HasAlternativeQuestChanges = true;
            }
            else
            {
                QuestType = Convert.ToUInt32(questType);
                QuestTarget = Convert.ToUInt16(questTarget);
                QuestTemplate = questTemplate;
                QuestTitle = questTitle;
                QuestConditions = questConditions;
                QuestRewards = questRewards;
                QuestTimestamp = questTimestamp;
                HasQuestChanges = true;
            }
        }

        public async Task<Dictionary<WebhookType, Pokestop>> UpdateAsync(MapContext context, bool updateQuest = false)
        {
            var webhooks = new Dictionary<WebhookType, Pokestop>();
            Pokestop? oldPokestop = null;
            try
            {
                oldPokestop = await context.Pokestops.FindAsync(Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pokestop: {ex}");
            }

            var now = DateTime.UtcNow.ToTotalSeconds();
            Updated = now;

            if (oldPokestop == null)
            {
                // Brand new Pokestop to insert, set first_seen_timestamp
                FirstSeenTimestamp = now;

                webhooks.Add(WebhookType.Pokestops, this);
                if (LureExpireTimestamp > 0)
                {
                    webhooks.Add(WebhookType.Lures, this);
                }
                if (QuestTimestamp > 0)
                {
                    webhooks.Add(WebhookType.Quests, this);
                }
                if (AlternativeQuestTimestamp > 0)
                {
                    webhooks.Add(WebhookType.AlternativeQuests, this);
                }
            }
            else
            {
                // Pokestop already exists, compare against this instance to see if anything needs
                // to be updated

                if (oldPokestop.CellId > 0 && CellId == 0)
                {
                    CellId = oldPokestop.CellId;
                }
                if (oldPokestop.Name != null && Name == null)
                {
                    Name = oldPokestop.Name;
                }
                if (oldPokestop.Url != null && Url == null)
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
                    QuestTitle = oldPokestop.QuestTitle;
                }
                if (updateQuest && oldPokestop.AlternativeQuestType != null && AlternativeQuestType == null)
                {
                    AlternativeQuestType = oldPokestop.AlternativeQuestType;
                    AlternativeQuestTarget = oldPokestop.AlternativeQuestTarget;
                    AlternativeQuestConditions = oldPokestop.AlternativeQuestConditions;
                    AlternativeQuestRewards = oldPokestop.AlternativeQuestRewards;
                    AlternativeQuestTimestamp = oldPokestop.AlternativeQuestTimestamp;
                    AlternativeQuestTemplate = oldPokestop.AlternativeQuestTemplate;
                    AlternativeQuestTitle = oldPokestop.AlternativeQuestTitle;
                }
                if (oldPokestop.LureId > 0 && LureId == 0)
                {
                    LureId = oldPokestop.LureId;
                }
                if ((oldPokestop.LureExpireTimestamp != null || oldPokestop.LureExpireTimestamp > 0) &&
                    (LureExpireTimestamp == null || LureExpireTimestamp == 0))
                {
                    LureExpireTimestamp = oldPokestop.LureExpireTimestamp;
                }

                // TODO: Check shouldUpdate

                if (oldPokestop.LureExpireTimestamp < LureExpireTimestamp)
                {
                    webhooks.Add(WebhookType.Lures, this);
                }
                if (updateQuest && (HasQuestChanges || QuestTimestamp > oldPokestop.QuestTimestamp))
                {
                    //HasQuestChanges = false;
                    webhooks.Add(WebhookType.Quests, this);
                }
                if (updateQuest && (HasAlternativeQuestChanges || AlternativeQuestTimestamp > oldPokestop.AlternativeQuestTimestamp))
                {
                    //HasAlternativeQuestChanges = false;
                    webhooks.Add(WebhookType.AlternativeQuests, this);
                }
            }

            return webhooks;
        }

        public dynamic? GetWebhookData(string type)
        {
            return type.ToLower() switch
            {
                "quest" => new
                {
                    type = WebhookHeaders.Quest,
                    message = new
                    {
                        pokestop_id = Id,
                        latitude = Latitude,
                        longitude = Longitude,
                        type = QuestType!,
                        target = QuestTarget!,
                        template = QuestTemplate!,
                        title = QuestTitle!,
                        conditions = QuestConditions!,
                        rewards = QuestRewards!,
                        updated = QuestTimestamp!,
                        pokestop_name = string.IsNullOrEmpty(Name) ? "Unknown" : Name,
                        pokestop_url = Url ?? "",
                        ar_scan_eligible = IsArScanEligible,
                        with_ar = true,
                    },
                },
                "alternative_quest" => new
                {
                    type = WebhookHeaders.Quest,
                    message = new
                    {
                        pokestop_id = Id,
                        latitude = Latitude,
                        longitude = Longitude,
                        type = AlternativeQuestType!,
                        target = AlternativeQuestTarget!,
                        template = AlternativeQuestTemplate!,
                        title = AlternativeQuestTitle!,
                        conditions = AlternativeQuestConditions!,
                        rewards = AlternativeQuestRewards!,
                        updated = AlternativeQuestTimestamp!,
                        pokestop_name = Name ?? UnknownPokestopName,
                        pokestop_url = Url ?? "",
                        ar_scan_eligible = IsArScanEligible,
                        with_ar = false,
                    },
                },
                _ => new
                {
                    type = WebhookHeaders.Pokestop,
                    message = new
                    {
                        pokestop_id = Id,
                        latitude = Latitude,
                        longitude = Longitude,
                        name = Name ?? UnknownPokestopName,
                        url = Url ?? "",
                        lure_expiration = LureExpireTimestamp ?? 0,
                        last_modified = LastModifiedTimestamp,
                        enabled = IsEnabled,
                        lure_id = LureId,
                        ar_scan_eligible = IsArScanEligible,
                        power_up_level = PowerUpLevel ?? 0,
                        power_up_points = PowerUpPoints ?? 0,
                        power_up_end_timestamp = PowerUpEndTimestamp ?? 0,
                        updated = Updated,
                    },
                },
            };
        }

        #endregion
    }
}