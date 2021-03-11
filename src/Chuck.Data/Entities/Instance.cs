﻿namespace Chuck.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    using Chuck.Data.Interfaces;

    [Table("instance")]
    public class Instance : BaseEntity, IAggregateRoot
    {
        [
            Column("name"),
            Key,
            JsonPropertyName("name"),
        ]
        public string Name { get; set; }

        [
            Column("type"),
            JsonPropertyName("type"),
        ]
        public InstanceType Type { get; set; }

        [
            Column("min_level"),
            JsonPropertyName("min_level"),
        ]
        public ushort MinimumLevel { get; set; }

        [
            Column("max_level"),
            JsonPropertyName("max_level"),
        ]
        public ushort MaximumLevel { get; set; }

        [
            Column("geofences"),
            JsonPropertyName("geofences"),
        ]
        public List<string> Geofences { get; set; }

        [
            Column("data"),
            JsonPropertyName("data"),
        ]
        public InstanceData Data { get; set; }

        [
            NotMapped,
            JsonPropertyName("count"),
        ]
        public int DeviceCount { get; set; }

        [
            NotMapped,
            JsonPropertyName("area_count"),
        ]
        public int AreaCount { get; set; }

        #region Helper Methods

        public static string InstanceTypeToString(InstanceType type)
        {
            return type switch
            {
                InstanceType.AutoQuest          => "auto_quest",
                InstanceType.CirclePokemon      => "circle_pokemon",
                InstanceType.CircleRaid         => "circle_raid",
                InstanceType.SmartCircleRaid    => "smart_raid",
                InstanceType.PokemonIV          => "pokemon_iv",
                InstanceType.Bootstrap          => "bootstrap",
                InstanceType.FindTTH            => "find_tth",
                _ => type.ToString(),
            };
        }

        public static InstanceType StringToInstanceType(string instanceType)
        {
            return (instanceType.ToLower()) switch
            {
                "auto_quest"        => InstanceType.AutoQuest,
                "circle_pokemon"    => InstanceType.CirclePokemon,
                "circle_raid"       => InstanceType.CircleRaid,
                "smart_raid"        => InstanceType.SmartCircleRaid,
                "pokemon_iv"        => InstanceType.PokemonIV,
                "bootstrap"         => InstanceType.Bootstrap,
                "find_tth"          => InstanceType.FindTTH,
                _ => InstanceType.CirclePokemon,
            };
        }

        #endregion
    }
}