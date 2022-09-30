namespace ChuckDeviceController.Data
{
    using Z.BulkOperations;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Data.Utilities;

    public static class BulkOptions
    {
        public static readonly BulkOperation<Pokemon> PokemonOnMergeUpdate = MySqlBulkUtils.GetBulkOptions<Pokemon>
        (
            ignoreOnMergeUpdateExpression: p => new
            {
                p.Id,
                p.PokemonId,
                p.Form,
                p.Costume,
                p.Gender,
                p.AttackIV,
                p.DefenseIV,
                p.StaminaIV,
                p.CP,
                p.Level,
                p.Size,
                p.Weight,
                p.Move1,
                p.Move2,
                p.Weather,
                p.PvpRankings,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Pokemon> PokemonIgnoreOnMerge = MySqlBulkUtils.GetBulkOptions<Pokemon>
        (
            ignoreOnMergeUpdateExpression: p => new
            {
                p.Id,
                p.PokemonId,
                p.Form,
                p.Costume,
                p.Gender,
                p.AttackIV,
                p.DefenseIV,
                p.StaminaIV,
                p.CP,
                p.Level,
                p.Size,
                p.Weight,
                p.Move1,
                p.Move2,
                p.Weather,
                p.PvpRankings,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Pokemon> PokemonOptions = MySqlBulkUtils.GetBulkOptions<Pokemon>
        (
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Pokestop> PokestopIgnoreOnMerge = MySqlBulkUtils.GetBulkOptions<Pokestop>
        (
            ignoreOnMergeUpdateExpression: p => new
            {
                p.Id,
                p.QuestType,
                p.QuestTitle,
                p.QuestTimestamp,
                p.QuestTemplate,
                p.QuestTarget,
                p.QuestRewardType,
                p.QuestRewards,
                p.QuestConditions,

                p.AlternativeQuestType,
                p.AlternativeQuestTitle,
                p.AlternativeQuestTimestamp,
                p.AlternativeQuestTemplate,
                p.AlternativeQuestTarget,
                p.AlternativeQuestRewardType,
                p.AlternativeQuestRewards,
                p.AlternativeQuestConditions,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Pokestop> PokestopDetailsOnMergeUpdate = MySqlBulkUtils.GetBulkOptions<Pokestop>
        (
            onMergeUpdateInputExpression: p => new
            {
                p.Id,
                p.Name,
                p.Url,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Incident> IncidentOptions = MySqlBulkUtils.GetBulkOptions<Incident>
        (
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Gym> GymOptions = MySqlBulkUtils.GetBulkOptions<Gym>
        (
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Gym> GymDetailsOnMergeUpdate = MySqlBulkUtils.GetBulkOptions<Gym>
        (
            onMergeUpdateInputExpression: p => new
            {
                p.Id,
                p.Name,
                p.Url,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<GymDefender> GymDefenderOptions = MySqlBulkUtils.GetBulkOptions<GymDefender>
        (
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<GymTrainer> GymTrainerOptions = MySqlBulkUtils.GetBulkOptions<GymTrainer>
        (
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Spawnpoint> SpawnpointOnMergeUpdate = MySqlBulkUtils.GetBulkOptions<Spawnpoint>
        (
            onMergeUpdateInputExpression: p => new
            {
                p.Id,
                p.LastSeen,
                p.Updated,
                p.DespawnSecond,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Cell> CellOnMergeUpdate = MySqlBulkUtils.GetBulkOptions<Cell>
        (
            onMergeUpdateInputExpression: p => new
            {
                p.Updated,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );

        public static readonly BulkOperation<Weather> WeatherOnMergeUpdate = MySqlBulkUtils.GetBulkOptions<Weather>
        (
            onMergeUpdateInputExpression: p => new
            {
                p.GameplayCondition,
                p.CloudLevel,
                p.FogLevel,
                p.RainLevel,
                p.SnowLevel,
                p.WindLevel,
                p.WindDirection,
                p.SpecialEffectLevel,
                p.Severity,
                p.WarnWeather,
                p.Updated,
            },
            allowDuplicateKeys: true,
            useTableLock: true
        );
    }
}