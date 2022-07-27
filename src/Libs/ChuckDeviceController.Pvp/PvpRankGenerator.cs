namespace ChuckDeviceController.Pvp
{
    using System.Text.Json;
    using System.Timers;

    using ChuckDeviceController.Pvp.Extensions;
    using ChuckDeviceController.Pvp.GameMaster;
    using ChuckDeviceController.Pvp.Models;

    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

    // Credits: https://github.com/RealDeviceMap/RealDeviceMap/blob/development/Sources/RealDeviceMapLib/Misc/PVPStatsManager.swift
    // Credits: https://github.com/WatWowMap/Chuck/blob/master/src/services/pvp.js
    // Credits: https://github.com/WatWowMap/Chuck/blob/master/src/services/pvp-core.js
    // Credits: https://github.com/Chuckleslove
    public class PvpRankGenerator : IPvpRankGenerator
    {
        #region Variables

        private Dictionary<PokemonWithFormAndGender, PokemonBaseStats> _pokemonBaseStats = new();
        private readonly Dictionary<PokemonWithFormAndGender, List<PvpRank>> _rankingLittle = new();
        private readonly Dictionary<PokemonWithFormAndGender, List<PvpRank>> _rankingGreat = new();
        private readonly Dictionary<PokemonWithFormAndGender, List<PvpRank>> _rankingUltra = new();

        private readonly object _littleLock = new();
        private readonly object _greatLock = new();
        private readonly object _ultraLock = new();

        private static readonly object _instanceLock = new();

        private readonly Timer _timer;

        private string? _lastETag;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        #endregion

        #region Singleton

        private static IPvpRankGenerator? _instance;
        public static IPvpRankGenerator Instance
        {
            get
            {
                // Lock singleton to prevent multiple instance creations
                // from different threads
                lock (_instanceLock)
                {
                    _instance ??= new PvpRankGenerator();
                }
                return _instance;
            }
        }


        #endregion

        #region Constructor

        public PvpRankGenerator()
        {
            _timer = new Timer(Strings.FetchMasterFileIntervalS * 1000);
            _timer.Elapsed += async (sender, e) => await LoadMasterFileIfNeededAsync();
            _timer.Start();

            LoadMasterFileAsync().ConfigureAwait(false)
                                 .GetAwaiter()
                                 .GetResult();
        }

        #endregion

        #region Public Methods

        public IReadOnlyList<PvpRank> GetPvpStats(HoloPokemonId pokemon, PokemonForm? form, IV iv, double level, PvpLeague league)
        {
            var pvpStats = GetTopPvpRanks(pokemon, form, league);
            if (pvpStats == null)
            {
                return new List<PvpRank>();
            }

            var rankings = new List<PvpRank>();
            PvpRank? lastRank = null;
            foreach (var levelCap in Strings.LevelCaps)
            {
                var competitionIndex = 0;
                var denseIndex = 0;
                var ordinalIndex = 0;
                var foundMatch = false;
                PvpRank? rank = null;
                var filteredStats = pvpStats.Where(stat => stat.Cap == levelCap).ToList();
                var shouldBreak = false;
                foreach (var ivStat in filteredStats)
                {
                    if (shouldBreak) break;

                    competitionIndex = ordinalIndex;
                    foreach (var ivLevel in ivStat.IVs)
                    {
                        if (ivLevel.IV == iv && ivLevel.Level >= level)
                        {
                            foundMatch = true;
                            rank = ivStat;
                            shouldBreak = true;
                            break;
                        }
                        ordinalIndex++;
                    }
                    denseIndex++;
                }

                if (!foundMatch)
                {
                    continue;
                }

                var max = Convert.ToDouble(filteredStats[0].CompetitionRank);
                var value = Convert.ToDouble(rank?.CompetitionRank);
                var currentIV = rank?.IVs.FirstOrDefault(iv => iv.IV == iv.IV);
                var ivs = currentIV == null
                    ? new()
                    : new List<PvpRank.IvWithCp> { currentIV };

                var lastStat = lastRank?.IVs.FirstOrDefault();
                var ivCpStat = ivs.FirstOrDefault();
                if (lastRank != null && lastStat != null &&
                    lastStat.Level == ivCpStat!.Level &&
                    lastRank.CompetitionRank == competitionIndex + 1 &&
                    lastStat.IV == ivCpStat.IV)
                {
                    var index = rankings.FindIndex(iv => iv.CompetitionRank == lastRank!.CompetitionRank);
                    if (index == -1)
                        continue; // Failed to find index

                    lastRank.IsCapped = true;
                    rankings[index] = lastRank;
                }
                else
                {
                    lastRank = new PvpRank
                    {
                        CompetitionRank = Convert.ToUInt32(competitionIndex + 1),
                        DenseRank = Convert.ToUInt32(denseIndex + 1),
                        OrdinalRank = Convert.ToUInt32(ordinalIndex + 1),
                        Percentage = value / max,
                        Cap = rank!.Cap,
                        IsCapped = false,
                        IVs = ivs,
                    };
                    rankings.Add(lastRank);
                }
            }
            return rankings;
        }

        public IReadOnlyDictionary<string, dynamic>? GetAllPvpLeagues(HoloPokemonId pokemon, PokemonForm? form, PokemonGender? gender, PokemonCostume? costume, IV iv, double level)
        {
            var pvp = new Dictionary<string, dynamic>();
            foreach (var leagueId in Enum.GetValues(typeof(PvpLeague)))
            {
                var league = (PvpLeague)leagueId;
                var leagueName = league.ToString().ToLower();
                var pvpStats = GetPvpStatsWithEvolutions(pokemon, form, gender, costume, iv, level, league)?
                    .Select(ranking =>
                    {
                        var rank = 0u;
                        var (pokemon, response) = ranking;
                        switch (Strings.DefaultRank)
                        {
                            case PvpRankType.Dense:
                                rank = response.DenseRank;
                                break;
                            case PvpRankType.Competition:
                                rank = response.CompetitionRank;
                                break;
                            case PvpRankType.Ordinal:
                                rank = response.OrdinalRank;
                                break;
                        }

                        var firstIV = response.IVs.FirstOrDefault();
                        var json = new
                        {
                            pokemon = pokemon.Pokemon,
                            form = pokemon.Form ?? 0,
                            gender = pokemon.Gender ?? 0,
                            rank,
                            percentage = response.Percentage,
                            cp = firstIV!.CP,
                            level = firstIV.Level,
                            competition_rank = response.CompetitionRank,
                            dense_rank = response.DenseRank,
                            ordinal_rank = response.OrdinalRank,
                            cap = response.Cap,
                            capped = response.IsCapped,
                        };
                        return json;
                    }).ToList();
                if ((pvpStats?.Count ?? 0) > 0)
                {
                    pvp[leagueName] = pvpStats!;
                }
            }
            return pvp.Count == 0
                ? null
                : pvp;
        }

        public IReadOnlyList<PvpRank> GetTopPvpRanks(HoloPokemonId pokemon, PokemonForm? form, PvpLeague league)
        {
            var info = new PokemonWithFormAndGender { Pokemon = pokemon, Form = form };
            List<PvpRank>? cached = null;

            switch (league)
            {
                case PvpLeague.Little:
                    lock (_littleLock)
                    {
                        if (_rankingLittle.ContainsKey(info))
                        {
                            cached = _rankingLittle[info];
                        }
                    }
                    break;
                case PvpLeague.Great:
                    lock (_greatLock)
                    {
                        if (_rankingGreat.ContainsKey(info))
                        {
                            cached = _rankingGreat[info];
                        }
                    }
                    break;
                case PvpLeague.Ultra:
                    lock (_ultraLock)
                    {
                        if (_rankingUltra.ContainsKey(info))
                        {
                            cached = _rankingUltra[info];
                        }
                    }
                    break;
            }

            if (cached == null)
            {
                if (!_pokemonBaseStats.ContainsKey(info))
                {
                    Console.WriteLine($"Nope");
                }
                var baseStats = _pokemonBaseStats[info];
                if (baseStats == null)
                {
                    return null;
                }
                var values = CalculateAllRanks(baseStats, (ushort)league);
                switch (league)
                {
                    case PvpLeague.Little:
                        lock (_littleLock)
                        {
                            _rankingLittle[info] = values;
                        }
                        break;
                    case PvpLeague.Great:
                        lock (_greatLock)
                        {
                            _rankingGreat[info] = values;
                        }
                        break;
                    case PvpLeague.Ultra:
                        lock (_ultraLock)
                        {
                            _rankingUltra[info] = values;
                        }
                        break;
                }
                return values;
            }
            return cached;
        }

        #endregion

        #region Private Methods

        private List<(PokemonWithFormAndGender, PvpRank)> GetPvpStatsWithEvolutions(HoloPokemonId pokemon, PokemonForm? form, PokemonGender? gender, PokemonCostume? costume, IV iv, double level, PvpLeague league)
        {
            var rankings = GetPvpStats(pokemon, form, iv, level, league);
            var result = rankings.Select(rank => (new PokemonWithFormAndGender { Pokemon = pokemon, Form = form, Gender = gender }, rank))
                                 .ToList();

            var pkmn = new PokemonWithFormAndGender { Pokemon = pokemon, Form = form };
            if (!_pokemonBaseStats.ContainsKey(pkmn))
            {
                Console.WriteLine($"Nope");
            }
            var baseStats = _pokemonBaseStats[pkmn];
            var hasNoEvolveForm = costume!.ToString().ToLower().Contains(Strings.NoEvolveForm);
            var hasCostumeEvoOverride = baseStats.CostumeEvolutionOverride != null &&
                baseStats.CostumeEvolutionOverride.Count > 0 &&
                costume != null && baseStats.CostumeEvolutionOverride.Contains(costume ?? null);

            // Check if Pokemon has evolutions but has form or costume that cannot evolve
            if (baseStats != null && baseStats.Evolutions != null &&
                baseStats.Evolutions.Count > 0 &&
                (hasNoEvolveForm || hasCostumeEvoOverride))
            {
                return result;
            }

            foreach (var evolution in baseStats!.Evolutions!)
            {
                if (evolution.Gender == null || evolution.Gender == gender)
                {
                    var pvpStats = GetPvpStatsWithEvolutions(evolution.Pokemon, evolution.Form, gender, costume, iv, level, league);
                    result.AddRange(pvpStats);
                }
            }
            return result;
        }

        private static List<PvpRank> CalculateAllRanks(PokemonBaseStats baseStats, ushort cpCap)
        {
            var rankings = new List<PvpRank>();
            foreach (var levelCap in Strings.LevelCaps)
            {
                var cp = baseStats.CalculateCP(IV.GetHundoCombination(), levelCap);
                // Check Pokemon CP is within league CP cap range
                if (cp <= Strings.LeagueFilters[cpCap])
                    continue;

                var pvpStats = CalculatePvpStat(baseStats, cpCap, levelCap);
                var keys = pvpStats.Keys.ToList();
                keys.Sort((a, b) => b.CompareTo(a));
                var sortedPvpStats = keys.Select(key => pvpStats[key]).ToList();
                rankings.AddRange(sortedPvpStats);
            }
            return rankings;
        }

        private static SortedDictionary<uint, PvpRank> CalculatePvpStat(PokemonBaseStats baseStats, ushort cpCap, ushort levelCap)
        {
            var ranking = new SortedDictionary<uint, PvpRank>();// new PvpRankComparer());
            var allCombinations = IV.GetAllCombinations();
            foreach (var iv in allCombinations)
            {
                double lowest = 1.0, highest = Convert.ToDouble(levelCap);
                uint bestCP = 0;
                while (lowest < highest)
                {
                    var mid = Math.Ceiling(lowest + highest) / 2;
                    var cp = baseStats.CalculateCP(iv, mid);
                    if (cp <= cpCap)
                    {
                        lowest = mid;
                        bestCP = cp;
                    }
                    else
                    {
                        highest = mid - 0.5;
                    }
                }
                if (lowest == 0)
                    continue;

                var value = baseStats.CalculateStatProduct(iv, lowest);
                if (!ranking.ContainsKey(value) || ranking[value] == null)
                {
                    ranking[value] = new PvpRank
                    {
                        CompetitionRank = value,
                        DenseRank = value,
                        OrdinalRank = value,
                        Percentage = 0.0,
                        Cap = levelCap,
                        IsCapped = false,
                        IVs = new(),
                    };
                }

                var ivWithCp = new PvpRank.IvWithCp(iv, lowest, bestCP);
                var index = ranking[value].IVs.FindIndex(iv => bestCP >= iv.CP);
                if (index > -1)
                {
                    ranking[value].IVs.Insert(index, ivWithCp);
                }
                else
                {
                    ranking[value].IVs.Add(ivWithCp);
                }
            }
            return ranking;
        }

        private static async Task<string?> GetETag(string url)
        {
            var request = await NetUtils.HeadAsync(url);
            if (request == null)
            {
                Console.WriteLine($"Failed to get eTag for game master file");
                return null;
            }
            var newETag = request.Headers.ETag?.Tag;
            return newETag;
        }

        #endregion

        #region Master File

        private async Task LoadMasterFileIfNeededAsync()
        {
            var newETag = await GetETag(Strings.MasterFileEndpoint);
            if (string.IsNullOrEmpty(newETag))
            {
                Console.WriteLine($"Failed to get HTTP header ETag from game master file request");
                return;
            }
            if (newETag != _lastETag)
            {
                Console.WriteLine($"Game master file changed, downloading new version...");
                await LoadMasterFileAsync();
            }
        }

        private async Task LoadMasterFileAsync()
        {
            Console.WriteLine($"Checking if game master file needs to be downloaded...");

            var newETag = await GetETag(Strings.MasterFileEndpoint);
            if (string.IsNullOrEmpty(newETag))
            {
                Console.WriteLine($"Failed to get HTTP header ETag from game master file request");
                return;
            }
            _lastETag = newETag;

            var requestData = await NetUtils.GetAsync(Strings.MasterFileEndpoint);

            Console.WriteLine($"Starting game master file parsing...");
            var templates = FromJson<List<Dictionary<string, object>>>(requestData);
            if (templates == null)
            {
                // Failed to parse templates
                Console.WriteLine($"Failed to deserialize game master file");
                return;
            }

            var pokemonBaseStats = new Dictionary<PokemonWithFormAndGender, PokemonBaseStats>();
            foreach (var template in templates)
            {
                var templateData = Convert.ToString(template["data"]);
                if (string.IsNullOrEmpty(templateData))
                {
                    // Failed
                    continue;
                }
                var data = FromJson<PokemonTemplate>(templateData);
                if (data == null)
                    continue;

                var templateId = data.TemplateId;
                if (string.IsNullOrEmpty(templateId))
                {
                    continue;
                }

                if (templateId.StartsWith("V") && templateId.Contains("_POKEMON_"))
                {
                    var pokemonInfo = data.PokemonSettings;
                    if (pokemonInfo == null)
                    {
                        // Skip templates that are not Pokemon
                        continue;
                    }

                    var pokemonName = pokemonInfo.PokemonId!; // <- Interesting, .NET has similar swift stuff now
                    var pokedexBaseStats = pokemonInfo.Stats;
                    var pokedexHeightM = pokemonInfo.PokedexHeightM;
                    var pokedexWeightKg = pokemonInfo.PokedexWeightKg;
                    var baseAttack = pokedexBaseStats.BaseAttack;
                    var baseDefense = pokedexBaseStats.BaseDefense;
                    var baseStamina = pokedexBaseStats.BaseStamina;
                    var pokemon = GetPokemonFromName(pokemonName);
                    if (pokemon == HoloPokemonId.Missingno)
                    {
                        Console.WriteLine($"Failed to get Pokemon for '{pokemonName}'");
                        // TODO: Should we just continue to allow the rest or exit all together?
                        continue;
                    }

                    var formName = pokemonInfo.Form;
                    PokemonForm? form = null;
                    if (!string.IsNullOrEmpty(formName))
                    {
                        var formId = GetFormFromName(formName);
                        if (formId == PokemonForm.Unset)
                        {
                            Console.WriteLine($"Failed to get form for '{formName}'");
                            continue;
                        }
                        form = formId;
                    }

                    var evolutions = new List<PokemonWithFormAndGender>();
                    var evolutionsInfo = pokemonInfo.EvolutionBranch;
                    if (evolutionsInfo != null)
                    {
                        foreach (var info in evolutionsInfo)
                        {
                            var evoName = info.Evolution;
                            if (string.IsNullOrEmpty(evoName))
                            {
                                // Skip
                                continue;
                            }
                            var evoPokemon = GetPokemonFromName(evoName);
                            if (!string.IsNullOrEmpty(evoName) && evoPokemon != HoloPokemonId.Missingno)
                            {
                                var evoFormName = info.Form;
                                var genderName = info.GenderRequirement;
                                PokemonForm? evoForm = string.IsNullOrEmpty(evoFormName)
                                    ? null
                                    : GetFormFromName(evoFormName);
                                PokemonGender? evoGender = string.IsNullOrEmpty(genderName)
                                    ? null
                                    : GetGenderFromName(genderName);
                                evolutions.Add(new PokemonWithFormAndGender { Pokemon = evoPokemon, Form = evoForm, Gender = evoGender });
                            }
                        }
                    }

                    var costumeEvolution = pokemonInfo.ObCostumeEvolution?
                        .Where(costumeName =>
                        {
                            var costume = GetCostumeFromName(costumeName);
                            return costume != PokemonCostume.Unset && costume != null;
                        })
                        .Select(GetCostumeFromName)
                        .ToList();
                    var baseStats = new PokemonBaseStats
                    {
                        BaseAttack = baseAttack,
                        BaseDefense = baseDefense,
                        BaseStamina = baseStamina,
                        Evolutions = evolutions,
                        BaseHeight = pokedexHeightM,
                        BaseWeight = pokedexWeightKg,
                        CostumeEvolutionOverride = costumeEvolution,
                    };
                    pokemonBaseStats[new PokemonWithFormAndGender { Pokemon = pokemon, Form = form }] = baseStats;
                }
            }

            _pokemonBaseStats = pokemonBaseStats;
            lock (_littleLock)
            {
                _rankingLittle.Clear();
            }
            lock (_greatLock)
            {
                _rankingGreat.Clear();
            }
            lock (_ultraLock)
            {
                _rankingUltra.Clear();
            }

            Console.WriteLine($"New game master file parsed successfully");
        }

        #endregion

        #region Helpers

        // TODO: Move to separate class
        private static HoloPokemonId GetPokemonFromName(string name)
        {
            var allPokemon = new List<HoloPokemonId>(Enum.GetValues<HoloPokemonId>());
            var pokemon = GetEnumFromName(name, allPokemon);
            return pokemon;
        }

        private static PokemonForm? GetFormFromName(string name)
        {
            var allForms = new List<PokemonForm>(Enum.GetValues<PokemonForm>());
            var form = GetEnumFromName(name, allForms);
            return form;
        }

        private static PokemonGender? GetGenderFromName(string name)
        {
            var allGenders = new List<PokemonGender>(Enum.GetValues<PokemonGender>());
            var gender = GetEnumFromName(name, allGenders);
            return gender;
        }

        private static PokemonCostume? GetCostumeFromName(string name)
        {
            var allCostumes = new List<PokemonCostume>(Enum.GetValues<PokemonCostume>());
            var costume = GetEnumFromName(name, allCostumes);
            return costume;
        }

        private static T? GetEnumFromName<T>(string name, List<T> values)
        {
            var lowerName = name.Replace("_", "").ToLower();
            var result = values.FirstOrDefault(x => x.ToString().ToLower() == lowerName);
            return result;
        }

        #endregion

        // TODO: Move eventually or reference ChuckDeviceController.Extensions library
        public static T? FromJson<T>(string json) =>
            JsonSerializer.Deserialize<T>(json, _jsonOptions);

        public static string ToJson<T>(T obj, bool pretty = false)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                WriteIndented = pretty,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            var json = JsonSerializer.Serialize(obj, options);
            return json;
        }

        /*
        private class PvpRankComparer : IComparer<uint>
        {
            public int Compare(uint x, uint y) => x.CompareTo(y);
        }
        */
    }
}