namespace ChuckDeviceConfigurator.Utilities;

using System.Text.Json.Serialization;

using static POGOProtos.Rpc.BelugaPokemonProto.Types;

using ChuckDeviceController.Extensions.Json;
using ChuckDeviceController.Net.Utilities;

public class GameMaster
{
    [JsonPropertyName("pokemon")]
    public IReadOnlyDictionary<uint, PokedexPokemon> Pokedex { get; set; }

    private static GameMaster? _instance;
    public static GameMaster Instance
    {
        get
        {
            if (_instance == null)
            {
                ReloadMasterFileAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            return _instance;
        }
    }

    public GameMaster()
    {
        Pokedex = new Dictionary<uint, PokedexPokemon>();
    }

    public PokedexPokemon GetPokemon(uint pokemonId, uint formId = 0)
    {
        if (pokemonId == 0)
            return null;

        if (!Instance.Pokedex.ContainsKey(pokemonId))
        {
            Console.WriteLine($"[Warning] Pokemon {pokemonId} does not exist in {Strings.MasterFileName}, please use an updated version.");
            return null;
        }

        var pkmn = Instance.Pokedex[pokemonId];
        var useForm = /*!pkmn.Attack.HasValue &&*/ formId > 0 && pkmn.Forms.ContainsKey(formId);
        var pkmnForm = useForm ? pkmn.Forms[formId] : pkmn;
        pkmnForm.Name = pkmn.Name;
        // Check if Pokemon is form and Pokemon types provided, if not use normal Pokemon types as fallback
        pkmnForm.Types = useForm && (pkmn.Forms[formId].Types?.Count ?? 0) > 0
            ? pkmn.Forms[formId].Types
            : pkmn.Types;
        return pkmnForm;
    }

    public static async Task ReloadMasterFileAsync()
    {
        await DownloadLatestMasterFile();
        try
        {
            _instance = LoadInit<GameMaster>(Strings.MasterFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
        }
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
            Console.WriteLine($"{filePath} file is empty.");
            return default;
        }

        return data.FromJson<T>();
    }

    private static async Task DownloadLatestMasterFile()
    {
        var data = await NetUtils.GetAsync(Strings.MasterFileEndpoint);
        if (string.IsNullOrEmpty(data))
        {
            return;
        }

        File.WriteAllText(Strings.MasterFilePath, data);
    }
}

public class PokedexPokemon
{
    [JsonPropertyName("pokedex_id")]
    public uint PokedexId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("forms")]
    public IReadOnlyDictionary<uint, PokedexPokemon> Forms { get; set; }

    /*
    [JsonPropertyName("default_form_id")]
    public int? DefaultFormId { get; set; }

    [JsonPropertyName("default_form")]
    public string DefaultForm { get; set; }
    */

    [JsonPropertyName("evolutions")]
    public IReadOnlyList<PokedexPokemonEvolution> Evolutions { get; set; }

    [JsonPropertyName("form")]
    public ushort? Form { get; set; }

    [JsonPropertyName("gender_requirement")]
    public PokemonGender GenderRequirement { get; set; }

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

    [JsonPropertyName("quick_moves")]
    public IReadOnlyList<string> QuickMoves { get; set; }

    [JsonPropertyName("charged_moves")]
    public IReadOnlyList<string> ChargedMoves { get; set; }

    [JsonPropertyName("candy_to_evolve")]
    public int? Candy { get; set; }

    [JsonPropertyName("buddy_distance")]
    public int? BuddyDistance { get; set; }

    [JsonPropertyName("legendary")]
    public bool Legendary { get; set; }

    [JsonPropertyName("mythic")]
    public bool Mythical { get; set; }

    [JsonPropertyName("buddy_distance_evolve")]
    public int? BuddyDistanceEvolve { get; set; }

    [JsonPropertyName("third_move_stardust")]
    public int ThirdMoveStardust { get; set; }

    [JsonPropertyName("third_move_candy")]
    public int ThirdMoveCandy { get; set; }

    [JsonPropertyName("gym_defender_eligible")]
    public bool GymDeployable { get; set; }

    [JsonPropertyName("gen_id")]
    public uint GenerationId { get; set; }

    [JsonPropertyName("generation")]
    public string? Generation { get; set; }

    [JsonPropertyName("temp_evolutions")]
    public IReadOnlyDictionary<uint, PokedexPokemon> TempEvolutions { get; set; }

    [JsonPropertyName("little")]
    public bool Little { get; set; }

    public PokedexPokemon()
    {
        Forms = new Dictionary<uint, PokedexPokemon>();
        Evolutions = new List<PokedexPokemonEvolution>();
        QuickMoves = new List<string>();
        ChargedMoves = new List<string>();
        Types = new List<PokemonType>();
        TempEvolutions = new Dictionary<uint, PokedexPokemon>();
    }
}

public class PokedexPokemonEvolution
{
    [JsonPropertyName("pokemon")]
    public uint PokemonId { get; set; }

    [JsonPropertyName("form")]
    public uint FormId { get; set; }

    [JsonPropertyName("gender_requirement")]
    public PokemonGender GenderRequirement { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PokemonType
{
    None = 0,
    Normal,
    Fighting,
    Flying,
    Poison,
    Ground,
    Rock,
    Bug,
    Ghost,
    Steel,
    Fire,
    Water,
    Grass,
    Electric,
    Psychic,
    Ice,
    Dragon,
    Dark,
    Fairy,
}