namespace ChuckDeviceController.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json.Serialization;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Extensions;

    using InvasionCharacter = POGOProtos.Rpc.EnumWrapper.Types.InvasionCharacter;

    public class GameMaster
    {
        private const string MasterFileName = "masterfile.json";
        private const string CpMultipliersFileName = "cp_multipliers.json";

        //private static readonly IEventLogger _logger = EventLogger.GetLogger("MASTER", Program.LogLevel);

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
        public IReadOnlyDictionary<double, double> CpMultipliers { get; }

        #region Singleton

        private static GameMaster _instance;
        public static GameMaster Instance
        {
            get
            {
                return _instance ??= LoadInit<GameMaster>(
                        Path.Combine(
                            Strings.DataFolder,
                            MasterFileName
                        )
                    );
            }
        }

        #endregion

        #endregion

        public GameMaster()
        {
            CpMultipliers = LoadInit<Dictionary<double, double>>(
                Path.Combine(
                    Strings.DataFolder,
                    CpMultipliersFileName
                )
            );
        }

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

        public static T LoadInit<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                //_logger.Error($"{filePath} database is empty.");
                Console.WriteLine($"{filePath} database is empty.");
                return default;
            }

            return data.FromJson<T>();
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