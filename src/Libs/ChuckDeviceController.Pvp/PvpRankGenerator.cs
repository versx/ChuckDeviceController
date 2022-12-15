namespace ChuckDeviceController.Pvp
{
    using System.Timers;

    using Microsoft.Extensions.Logging;
    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

    using ChuckDeviceController.Extensions.Json;
    using ChuckDeviceController.Logging;
    using ChuckDeviceController.Net.Utilities;
    using ChuckDeviceController.Pvp.Extensions;
    using ChuckDeviceController.Pvp.GameMaster;
    using ChuckDeviceController.Pvp.Models;

    // Credits: https://github.com/Chuckleslove
    // Credits: https://github.com/RealDeviceMap/RealDeviceMap/blob/development/Sources/RealDeviceMapLib/Misc/PVPStatsManager.swift
    // Credits: https://github.com/WatWowMap/Chuck/blob/master/src/services/pvp.js
    // Credits: https://github.com/WatWowMap/Chuck/blob/master/src/services/pvp-core.js
    public class PvpRankGenerator : IPvpRankGenerator
    {
        #region Variables

        private static readonly ILogger<IPvpRankGenerator> _logger =
            GenericLoggerFactory.CreateLogger<IPvpRankGenerator>();
        private Dictionary<PokemonWithFormAndGender, PokemonBaseStats> _pokemonBaseStats = new();
        private readonly Dictionary<PvpLeague, Dictionary<PokemonWithFormAndGender, List<PvpRank>>> _ranking = new();
        private readonly object _rankLock = new();
        private static readonly object _instanceLock = new();

        private readonly Timer _timer;
        private string? _lastETag;
        private static bool _loading;

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
                    return _instance;
                }
            }
        }

        #endregion

        #region Constructor

        public PvpRankGenerator()
        {
            _timer = new Timer(Strings.FetchMasterFileIntervalM * 60 * 1000);
            _timer.Elapsed += async (sender, e) => await LoadMasterFileIfNeededAsync();
        }

        #endregion

        #region Public Methods

        public async Task InitializeAsync()
        {
            if (!_timer.Enabled)
            {
                _timer.Start();
            }

            await LoadMasterFileAsync();
        }

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
            foreach (var leagueId in System.Enum.GetValues(typeof(PvpLeague)))
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

        public IReadOnlyList<PvpRank>? GetTopPvpRanks(HoloPokemonId pokemon, PokemonForm? form, PvpLeague league)
        {
            var info = new PokemonWithFormAndGender { Pokemon = pokemon, Form = form };
            List<PvpRank>? cached = null;

            lock (_rankLock)
            {
                if (_ranking.ContainsKey(league))
                {
                    if (_ranking[league].ContainsKey(info))
                    {
                        cached = _ranking[league][info];
                    }
                }
            }

            if (!(cached?.Any() ?? false))
            {
                if (!_pokemonBaseStats.ContainsKey(info))
                {
                    return null;
                }
                var baseStats = _pokemonBaseStats[info];
                var values = CalculateAllRanks(baseStats, (ushort)league);
                lock (_rankLock)
                {
                    if (!_ranking.ContainsKey(league))
                    {
                        _ranking.Add(league, new());
                    }
                    if (!_ranking[league].ContainsKey(info))
                    {
                        _ranking[league].Add(info, values);
                    }
                    else
                    {
                        _ranking[league][info] = values;
                    }
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
                _logger.LogWarning($"Pokemon base stats manifest does not contains Pokemon '{pkmn.Pokemon}_{pkmn.Form}_{pkmn.Gender}', skipping...");
                return result;
                //return null;
            }
            var baseStats = _pokemonBaseStats[pkmn];
            var hasNoEvolveForm = (costume ?? PokemonCostume.Unset).ToString().ToLower().Contains(Strings.NoEvolveForm);
            var hasCostumeEvoOverride = baseStats.CostumeEvolutionOverride != null &&
                baseStats.CostumeEvolutionOverride.Count > 0 &&
                costume != null && baseStats.CostumeEvolutionOverride.Contains(costume ?? null);

            // Check if Pokemon has evolutions but has form or costume that cannot evolve
            if ((baseStats.Evolutions?.Count ?? 0) > 0 && (hasNoEvolveForm || hasCostumeEvoOverride))
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
            var newETag = request?.Headers?.ETag?.Tag;
            return newETag;
        }

        #endregion

        #region Master File

        private async Task LoadMasterFileIfNeededAsync()
        {
            if (_loading)
                return;

            var newETag = await GetETag(Strings.MasterFileEndpoint);
            if (string.IsNullOrEmpty(newETag))
            {
                _logger.LogWarning($"Failed to get HTTP header ETag from game master file request");
                return;
            }

            if (newETag == _lastETag)
                return;

            _logger.LogWarning($"Game master file changed, downloading new version...");
            await LoadMasterFileAsync(newETag);
        }

        public async Task LoadMasterFileAsync(string? eTag = null)
        {
            if (_loading)
                return;

            _loading = true;
            _logger.LogDebug($"Checking if game master file needs to be downloaded...");

            var newETag = eTag ?? await GetETag(Strings.MasterFileEndpoint);
            if (string.IsNullOrEmpty(newETag))
            {
                _logger.LogWarning($"Failed to get HTTP header ETag from game master file request");
                return;
            }
            _lastETag = newETag;

            var requestData = await NetUtils.GetAsync(Strings.MasterFileEndpoint);
            if (string.IsNullOrEmpty(requestData))
            {
                _logger.LogWarning($"Failed to download latest game master file data.");
                return;
            }

            _logger.LogInformation($"Starting game master file parsing...");
            var templates = requestData.FromJson<List<Dictionary<string, object>>>();
            if (templates == null)
            {
                // Failed to parse templates
                _logger.LogError($"Failed to deserialize game master file");
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
                var data = templateData.FromJson<PokemonTemplate>();
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
                    var pokemon = pokemonName.GetPokemonFromName();
                    if (pokemon == HoloPokemonId.Missingno)
                    {
                        _logger.LogDebug($"Failed to get Pokemon for '{pokemonName}'");
                        continue;
                    }

                    var formName = pokemonInfo.Form;
                    PokemonForm? form = null;
                    if (!string.IsNullOrEmpty(formName))
                    {
                        var formId = formName.GetFormFromName();
                        if (formId == PokemonForm.Unset)
                        {
                            _logger.LogDebug($"Failed to get form for '{formName}'");
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
                            var evoPokemon = evoName.GetPokemonFromName();
                            if (!string.IsNullOrEmpty(evoName) && evoPokemon != HoloPokemonId.Missingno)
                            {
                                var evoFormName = info.Form;
                                var genderName = info.GenderRequirement;
                                PokemonForm? evoForm = string.IsNullOrEmpty(evoFormName)
                                    ? null
                                    : evoFormName.GetFormFromName();
                                PokemonGender? evoGender = string.IsNullOrEmpty(genderName)
                                    ? null
                                    : genderName.GetGenderFromName();
                                evolutions.Add(new PokemonWithFormAndGender { Pokemon = evoPokemon, Form = evoForm, Gender = evoGender });
                            }
                        }
                    }

                    var costumeEvolution = pokemonInfo.ObCostumeEvolution?
                        .Where(costumeName =>
                        {
                            var costume = costumeName.GetCostumeFromName();
                            return costume != PokemonCostume.Unset && costume != null;
                        })
                        .Select(PokemonExtensions.GetCostumeFromName)
                        .ToList();
                    var baseStats = new PokemonBaseStats
                    {
                        BaseAttack = baseAttack,
                        BaseDefense = baseDefense,
                        BaseStamina = baseStamina,
                        Evolutions = evolutions,
                        BaseHeight = pokedexHeightM,
                        BaseWeight = pokedexWeightKg,
                        CostumeEvolutionOverride = costumeEvolution!,
                    };
                    pokemonBaseStats[new PokemonWithFormAndGender { Pokemon = pokemon, Form = form }] = baseStats;
                }
            }

            _pokemonBaseStats = pokemonBaseStats;

            lock (_rankLock)
            {
                _ranking.Clear();
            }

            _loading = false;
            _logger.LogInformation($"New game master file parsed successfully");
        }

        #endregion
    }
}