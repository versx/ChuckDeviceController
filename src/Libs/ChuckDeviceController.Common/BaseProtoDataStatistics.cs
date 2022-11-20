namespace ChuckDeviceController.Common
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public interface IProtoDataStatistics
    {
        ulong TotalRequestsProcessed { get; }

        uint TotalProtoPayloadsReceived { get; }

        uint TotalProtosProcessed { get; }

        uint TotalEntitiesProcessed { get; }

        uint TotalEntitiesUpserted { get; }

        uint TotalPlayerDataProcessed { get; }

        uint TotalS2CellsProcessed { get; }

        uint TotalClientWeatherCellsProcessed { get; }

        uint TotalFortsProcessed { get; }

        uint TotalFortDetailsProcessed { get; }

        uint TotalGymInfoProcessed { get; }

        uint TotalGymDefendersProcessed { get; }

        uint TotalGymTrainersProcessed { get; }

        uint TotalIncidentsProcessed { get; }

        uint TotalWildPokemonProcessed { get; }

        uint TotalNearbyPokemonProcessed { get; }

        uint TotalMapPokemonProcessed { get; }

        uint TotalQuestsProcessed { get; }

        uint TotalPokemonEncountersProcessed { get; }

        uint TotalPokemonDiskEncountersProcessed { get; }

        uint TotalSpawnpointsProcessed { get; }

        IReadOnlyList<DataEntityTime> Times { get; }

        DataEntityTime? AverageTime { get; }
    }

    public abstract class BaseProtoDataStatistics : IProtoDataStatistics
    {
        [JsonPropertyName("total_requests")]
        [DisplayName("Requests Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual ulong TotalRequestsProcessed { get; set; }

        [JsonPropertyName("protos_received")]
        [DisplayName("Proto Payloads Received")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalProtoPayloadsReceived { get; set; }

        [JsonPropertyName("protos_processed")]
        [DisplayName("Protos Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalProtosProcessed { get; set; }

        [JsonPropertyName("entities_processed")]
        [DisplayName("Entities Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalEntitiesProcessed { get; set; }

        [JsonPropertyName("entities_upserted")]
        [DisplayName("Entities Upserted")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalEntitiesUpserted { get; set; }

        [JsonPropertyName("player_data_processed")]
        [DisplayName("Player Data Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalPlayerDataProcessed { get; set; }

        [JsonPropertyName("s2_cells_processed")]
        [DisplayName("S2 Cells Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalS2CellsProcessed { get; set; }

        [JsonPropertyName("weather_cells_processed")]
        [DisplayName("Weather Cells Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalClientWeatherCellsProcessed { get; set; }

        [JsonPropertyName("forts_processed")]
        [DisplayName("Forts Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalFortsProcessed { get; set; }

        [JsonPropertyName("fort_details_processed")]
        [DisplayName("Fort Details Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalFortDetailsProcessed { get; set; }

        [JsonPropertyName("gym_info_processed")]
        [DisplayName("Gym Info Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalGymInfoProcessed { get; set; }

        [JsonPropertyName("gym_defenders_processed")]
        [DisplayName("Gym Defenders Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalGymDefendersProcessed { get; set; }

        [JsonPropertyName("gym_trainers_processed")]
        [DisplayName("Gym Trainers Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalGymTrainersProcessed { get; set; }

        [JsonPropertyName("incidents_processed")]
        [DisplayName("Incidents Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalIncidentsProcessed { get; set; }

        [JsonPropertyName("wild_pokemon_processed")]
        [DisplayName("Wild Pokemon Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalWildPokemonProcessed { get; set; }

        [JsonPropertyName("nearby_pokemon_processed")]
        [DisplayName("Nearby Pokemon Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalNearbyPokemonProcessed { get; set; }

        [JsonPropertyName("map_pokemon_processed")]
        [DisplayName("Map Pokemon Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalMapPokemonProcessed { get; set; }

        [JsonPropertyName("quests_processed")]
        [DisplayName("Quests Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalQuestsProcessed { get; set; }

        [JsonPropertyName("pokemon_encounters_processed")]
        [DisplayName("Pokemon Encounters Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalPokemonEncountersProcessed { get; set; }

        [JsonPropertyName("pokemon_disk_encounters_processed")]
        [DisplayName("Pokemon Disk Encounters Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalPokemonDiskEncountersProcessed { get; set; }

        [JsonPropertyName("spawnpoints_processed")]
        [DisplayName("Spawnpoints Processed")]
        [DisplayFormat(DataFormatString = "{0:N0}")]
        public virtual uint TotalSpawnpointsProcessed { get; set; }

        [JsonPropertyName("data_times")]
        [DisplayName("Benchmark Times")]
        public virtual IReadOnlyList<DataEntityTime> Times { get; set; }

        [JsonPropertyName("avg_time")]
        [DisplayName("Average Benchmark Time")]
        public virtual DataEntityTime? AverageTime { get; set; }

        public BaseProtoDataStatistics()
        {
            Times = new List<DataEntityTime>();
        }
    }
}