namespace ChuckDeviceController.Services.Models
{
    using System;
    using System.Collections.Generic;

    using POGOProtos.Rpc;

    public class ConsumerData
    {
        public List<dynamic> WildPokemon { get; set; }

        public List<dynamic> NearbyPokemon { get; set; }

        public List<ClientWeatherProto> ClientWeather { get; set; }

        public List<dynamic> Forts { get; set; }

        public List<FortDetailsOutProto> FortDetails { get; set; }

        public List<GymGetInfoOutProto> GymInfo { get; set; }

        public List<QuestProto> Quests { get; set; }

        public List<FortSearchOutProto> FortSearch { get; set; }

        public List<dynamic> Encounters { get; set; }

        public List<ulong> Cells { get; set; }

        //public List<Spawnpoint> Spawnpoints { get; set; }

        public List<InventoryDeltaProto> Inventory { get; set; }

        public List<dynamic> PlayerData { get; set; }

        public string Username { get; set; }

        public string Uuid { get; set; }

        public ConsumerData()
        {
            WildPokemon = new List<dynamic>();
            NearbyPokemon = new List<dynamic>();
            ClientWeather = new List<ClientWeatherProto>();
            Forts = new List<dynamic>();
            FortDetails = new List<FortDetailsOutProto>();
            GymInfo = new List<GymGetInfoOutProto>();
            Quests = new List<QuestProto>();
            FortSearch = new List<FortSearchOutProto>();
            Encounters = new List<dynamic>();
            Cells = new List<ulong>();
            //Spawnpoints = new List<Spawnpoint>();
            Inventory = new List<InventoryDeltaProto>();
            PlayerData = new List<dynamic>();
        }
    }
}