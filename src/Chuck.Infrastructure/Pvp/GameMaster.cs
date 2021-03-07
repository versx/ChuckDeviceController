namespace Chuck.Infrastructure.Pvp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    using Chuck.Infrastructure.Extensions;
    using Chuck.Infrastructure.Utilities;

    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;

    public class GameMaster
    {
        private const string LatestGameMasterUrl = "https://raw.githubusercontent.com/WatWowMap/Masterfile-Generator/master/master-latest.json";

        #region Singleton

        private static GameMaster _instance;
        public static GameMaster Instance =>
            _instance ??= DownloadLatestGameMaster();

        #endregion

        #region Properties

        [JsonPropertyName("pokemon")]
        public IReadOnlyDictionary<uint, PokedexPokemon> Pokedex { get; set; }

        //[JsonProperty("moves")]
        //public IReadOnlyDictionary<int, Moveset> Movesets { get; set; }

        [JsonPropertyName("quest_conditions")]
        public IReadOnlyDictionary<string, QuestConditionModel> QuestConditions { get; set; }

        [JsonPropertyName("quest_types")]
        public IReadOnlyDictionary<int, QuestTypeModel> QuestTypes { get; set; }

        [JsonPropertyName("quest_reward_types")]
        public IReadOnlyDictionary<int, QuestRewardTypeModel> QuestRewardTypes { get; set; }

        [JsonPropertyName("throw_types")]
        public IReadOnlyDictionary<int, string> ThrowTypes { get; set; }

        [JsonPropertyName("items")]
        public IReadOnlyDictionary<int, ItemModel> Items { get; set; }

        [JsonPropertyName("grunt_types")]
        public IReadOnlyDictionary<InvasionCharacter, TeamRocketInvasion> GruntTypes { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<double, double> CpMultipliers { get; } = new Dictionary<double, double>
        {
            { 1, 0.09399999678134918 },
            { 1.5, 0.13513743132352830 },
            { 2, 0.16639786958694458 },
            { 2.5, 0.19265091419219970 },
            { 3, 0.21573247015476227 },
            { 3.5, 0.23657265305519104 },
            { 4, 0.25572004914283750 },
            { 4.5, 0.27353037893772125 },
            { 5, 0.29024988412857056 },
            { 5.5, 0.30605737864971160 },
            { 6, 0.32108759880065920 },
            { 6.5, 0.33544503152370453 },
            { 7, 0.34921267628669740 },
            { 7.5, 0.36245773732662200 },
            { 8, 0.37523558735847473 },
            { 8.5, 0.38759241108516856 },
            { 9, 0.39956727623939514 },
            { 9.5, 0.41119354951725060 },
            { 10, 0.4225000143051148 },
            { 10.5, 0.4329264134104144 },
            { 11, 0.4431075453758240 },
            { 11.5, 0.4530599538719858 },
            { 12, 0.4627983868122100 },
            { 12.5, 0.4723360780626535 },
            { 13, 0.4816849529743195 },
            { 13.5, 0.4908558102324605 },
            { 14, 0.4998584389686584 },
            { 14.5, 0.5087017565965652 },
            { 15, 0.5173939466476440 },
            { 15.5, 0.5259425118565559 },
            { 16, 0.5343543291091919 },
            { 16.5, 0.5426357612013817 },
            { 17, 0.5507926940917969 },
            { 17.5, 0.5588305993005633 },
            { 18, 0.5667545199394226 },
            { 18.5, 0.5745691470801830 },
            { 19, 0.5822789072990417 },
            { 19.5, 0.5898879119195044 },
            { 20, 0.5974000096321106 },
            { 20.5, 0.6048236563801765 },
            { 21, 0.6121572852134705 },
            { 21.5, 0.6194041110575199 },
            { 22, 0.6265671253204346 },
            { 22.5, 0.6336491815745830 },
            { 23, 0.6406529545783997 },
            { 23.5, 0.6475809663534164 },
            { 24, 0.6544356346130370 },
            { 24.5, 0.6612192690372467 },
            { 25, 0.6679340004920960 },
            { 25.5, 0.6745819002389908 },
            { 26, 0.6811649203300476 },
            { 26.5, 0.6876849085092545 },
            { 27, 0.6941436529159546 },
            { 27.5, 0.7005428969860077 },
            { 28, 0.7068842053413391 },
            { 28.5, 0.7131690979003906 },
            { 29, 0.7193990945816040 },
            { 29.5, 0.7255756109952927 },
            { 30, 0.7317000031471252 },
            { 30.5, 0.7347410172224045 },
            { 31, 0.7377694845199585 },
            { 31.5, 0.7407855764031410 },
            { 32, 0.7437894344329834 },
            { 32.5, 0.7467812150716782 },
            { 33, 0.7497610449790955 },
            { 33.5, 0.7527291029691696 },
            { 34, 0.7556855082511902 },
            { 34.5, 0.7586303651332855 },
            { 35, 0.7615638375282288 },
            { 35.5, 0.7644860669970512 },
            { 36, 0.7673971652984619 },
            { 36.5, 0.7702972739934921 },
            { 37, 0.7731865048408508 },
            { 37.5, 0.7760649472475052 },
            { 38, 0.7789327502250671 },
            { 38.5, 0.78179006 },
            { 39, 0.78463697 },
            { 39.5, 0.78747358 },
            { 40, 0.79030001 },
            { 40.5, 0.792803950958808 },
            { 41, 0.795300006866455 },
            { 41.5, 0.797803921486970 },
            { 42, 0.800300002098084 },
            { 42.5, 0.802803892322847 },
            { 43, 0.805299997329712 },
            { 43.5, 0.807803863460723 },
            { 44, 0.810299992561340 },
            { 44.5, 0.812803834895027 },
            { 45, 0.815299987792969 },
            { 45.5, 0.817803806620319 },
            { 46, 0.820299983024597 },
            { 46.5, 0.822803778631297 },
            { 47, 0.825299978256226 },
            { 47.5, 0.827803750922783 },
            { 48, 0.830299973487854 },
            { 48.5, 0.832803753381377 },
            { 49, 0.835300028324127 },
            { 49.5, 0.837803755931570 },
            { 50, 0.840300023555756 },
            { 50.5, 0.842803729034748 },
            { 51, 0.845300018787384 },
            { 51.5, 0.847803702398935 },
            { 52, 0.850300014019012 },
            { 52.5, 0.852803676019539 },
            { 53, 0.855300009250641 },
            { 53.5, 0.857803649892077 },
            { 54, 0.860300004482269 },
            { 54.5, 0.862803624012169 },
            { 55, 0.865299999713897 }
        };

        #endregion

        public static PokedexPokemon GetPokemon(uint pokemonId, uint formId)
        {
            if (!Instance.Pokedex.ContainsKey(pokemonId))
                return null;

            var pkmn = Instance.Pokedex[pokemonId];
            var useForm = !pkmn.Attack.HasValue && formId > 0 && pkmn.Forms.ContainsKey(formId);
            var pkmnForm = useForm ? pkmn.Forms[formId] : pkmn;
            pkmnForm.Name = pkmn.Name;
            return pkmnForm;
        }

        private static GameMaster DownloadLatestGameMaster()
        {
            var data = NetUtils.Download(LatestGameMasterUrl);
            if (string.IsNullOrEmpty(data))
            {
                ConsoleExt.WriteError($"Failed to download latest game master from: {LatestGameMasterUrl}");
                return default;
            }
            return data.FromJson<GameMaster>();
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PokemonType
    {
        None = 0,
        Normal = 1,
        Fighting = 2,
        Flying = 3,
        Poison = 4,
        Ground = 5,
        Rock = 6,
        Bug = 7,
        Ghost = 8,
        Steel = 9,
        Fire = 10,
        Water = 11,
        Grass = 12,
        Electric = 13,
        Psychic = 14,
        Ice = 15,
        Dragon = 16,
        Dark = 17,
        Fairy = 18
    }

    public class PokedexPokemon
    {
        [JsonPropertyName("pokedex_id")]
        public uint PokedexId { get; set; }

        [JsonPropertyName("pokemon")]
        public uint Pokemon { get; set; } // Used for evolutions, why wouldn't you just use the same value as the base pokemon i.e. pokedex_id :facepalm:

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("forms")]
        public IReadOnlyDictionary<uint, PokedexPokemon> Forms { get; set; }

        [JsonPropertyName("default_form_id")]
        public int? DefaultFormId { get; set; }

        [JsonIgnore]
        public PokedexPokemon DefaultForm => DefaultFormId > 0 && Forms.ContainsKey((uint)(DefaultFormId ?? 0))
            ? Forms[(uint)(DefaultFormId ?? 0)]
            : this;

        [JsonPropertyName("form")]
        public uint FormId { get; set; } // Used for evolutions

        [JsonPropertyName("evolutions")]
        public IReadOnlyList<PokedexPokemon> Evolutions { get; set; }

        [JsonPropertyName("temp_evolutions")]
        public IReadOnlyDictionary<uint, PokedexPokemon> TempEvolutions { get; set; }

        //[JsonPropertyName("form")]
        //public string Form { get; set; }

        [JsonPropertyName("types")]
        public IReadOnlyList<PokemonType> Types { get; set; }

        [JsonPropertyName("attack")]
        public int? Attack { get; set; }

        [JsonPropertyName("defense")]
        public int? Defense { get; set; }

        [JsonPropertyName("stamina")]
        public int? Stamina { get; set; }

        [JsonPropertyName("height")]
        public double? Height { get; set; }

        [JsonPropertyName("weight")]
        public double? Weight { get; set; }

        [JsonPropertyName("flee_rate")]
        public double? FleeRate { get; set; }

        [JsonPropertyName("capture_rate")]
        public double? CaptureRate { get; set; }

        [JsonPropertyName("quick_moves")]
        public IReadOnlyList<string> QuickMoves { get; set; }

        [JsonPropertyName("charged_moves")]
        public IReadOnlyList<string> ChargedMoves { get; set; }

        [JsonPropertyName("candy_to_evolve")]
        public int? Candy { get; set; }

        [JsonPropertyName("legendary")]
        public bool IsLegendary { get; set; }

        [JsonPropertyName("mythic")]
        public bool IsMythical { get; set; }

        [JsonPropertyName("buddy_group_number")]
        public int BuddyGroupNumber { get; set; }

        [JsonPropertyName("buddy_distance")]
        public int? BuddyDistance { get; set; }

        [JsonPropertyName("third_move_stardust")]
        public int ThirdMoveStardust { get; set; }

        [JsonPropertyName("third_move_candy")]
        public int ThirdMoveCandy { get; set; }

        [JsonPropertyName("gym_defender_eligible")]
        public bool IsGymDefenderEligible { get; set; }

        [JsonPropertyName("gender_requirement")]
        public PokemonGender GenderRequirement { get; set; }

        [JsonPropertyName("unreleased")]
        public bool IsUnreleased { get; set; }

        public PokedexPokemon()
        {
            Forms = new Dictionary<uint, PokedexPokemon>();
            Evolutions = new List<PokedexPokemon>();
            TempEvolutions = new Dictionary<uint, PokedexPokemon>();
            QuickMoves = new List<string>();
            ChargedMoves = new List<string>();
            Types = new List<PokemonType>();
        }
    }

    public class ItemModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("proto")]
        public string ProtoName { get; set; }

        [JsonPropertyName("min_trainer_level")]
        public int MinimumTrainerLevel { get; set; }
    }

    public class QuestTypeModel
    {
        [JsonPropertyName("prototext")]
        public string ProtoText { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class QuestConditionModel
    {
        [JsonPropertyName("prototext")]
        public string ProtoText { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class QuestRewardTypeModel
    {
        [JsonPropertyName("prototext")]
        public string ProtoText { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class TeamRocketInvasion
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("grunt")]
        public string Grunt { get; set; }

        [JsonPropertyName("second_reward")]
        public string SecondReward { get; set; } // Convert to boolean in masterfile

        [JsonPropertyName("encounters")]
        public TeamRocketEncounters Encounters { get; set; }

        [JsonIgnore]
        public bool HasEncounter => Encounters?.First?.Count > 0 || Encounters?.Second?.Count > 0 || Encounters?.Third?.Count > 0;

        public TeamRocketInvasion()
        {
            Encounters = new TeamRocketEncounters();
        }
    }

    public class TeamRocketEncounters
    {
        [JsonPropertyName("first")]
        public List<string> First { get; set; }

        [JsonPropertyName("second")]
        public List<string> Second { get; set; }

        [JsonPropertyName("third")]
        public List<string> Third { get; set; }

        public TeamRocketEncounters()
        {
            First = new List<string>();
            Second = new List<string>();
            Third = new List<string>();
        }
    }
}