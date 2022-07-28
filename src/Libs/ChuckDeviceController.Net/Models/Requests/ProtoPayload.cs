namespace ChuckDeviceController.Net.Models.Requests
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class ProtoPayload
    {
        [JsonPropertyName("pokemon_encounter_id")]
        public string? PokemonEncounterId { get; set; }

        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("devicename")]
        public string? DeviceName { get; set; }

        [JsonPropertyName("uuid_control")]
        public string? UuidControl { get; set; }

        [JsonPropertyName("lat_target")]
        public double LatitudeTarget { get; set; }

        [JsonPropertyName("lon_target")]
        public double LongitudeTarget { get; set; }

        [JsonPropertyName("contents")]
        public IReadOnlyList<ProtoData> Contents { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("trainerlvl")]
        public ushort Level { get; set; }

        [JsonPropertyName("trainerexp")]
        public ulong? TrainerXp { get; set; }

        [JsonPropertyName("target_max_distnace")] // TODO: Typflo, fix eventually
        public uint? TargetMaxDistance { get; set; }

        //[JsonPropertyName("pokemon_encounter_id_for_encounter")]
        //public string? PokemonEncounterIdForEncounter { get; set; }

        [JsonPropertyName("timestamp")]
        public ulong? Timestamp { get; set; }

        [JsonPropertyName("have_ar")]
        public bool? HaveAr { get; set; }
    }

    public class ProtoData
    {
        [JsonPropertyName("method")]
        public int Method { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("have_ar")]
        public bool? HaveAr { get; set; }
    }
}