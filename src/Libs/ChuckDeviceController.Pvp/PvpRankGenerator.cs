namespace ChuckDeviceController.Pvp
{
    using System.Text.Json;
    using System.Timers;

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

        private Dictionary<PokemonWithFormAndGender, PokemonBaseStats> _stats = new();
        private readonly Dictionary<PokemonWithFormAndGender, List<PvpRank>> _rankingLittle = new();
        private readonly Dictionary<PokemonWithFormAndGender, List<PvpRank>> _rankingGreat = new();
        private readonly Dictionary<PokemonWithFormAndGender, List<PvpRank>> _rankingUltra = new();

        private readonly object _littleLock = new();
        private readonly object _greatLock = new();
        private readonly object _ultraLock = new();

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
        public static IPvpRankGenerator Instance =>
            _instance ??= new PvpRankGenerator();


        #endregion

        #region Constructor

        public PvpRankGenerator()
        {
            _timer = new Timer(Strings.FetchMasterFileIntervalS * 1000);
            _timer.Elapsed += async (sender, e) => await LoadMasterFileIfNeededAsync();
            LoadMasterFileIfNeededAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            /*
            _timer.Start();

            LoadMasterFileAsync().ConfigureAwait(false)
                                 .GetAwaiter()
                                 .GetResult();
            */
        }

        #endregion

        #region Public Methods

        public List<PvpRank> GetPvpStats(HoloPokemonId pokemon, PokemonForm form, IV iv, double level, PvpLeague league)
        {
            var stats = GetTopPvpRanks(pokemon, form, league);
            if (stats == null)
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
                var filteredStats = stats.Where(stat => stat.Cap == levelCap).ToList();
                foreach (var ivStat in filteredStats)
                {
                    competitionIndex = ordinalIndex;
                    foreach (var ivLevel in ivStat.IVs)
                    {
                        if (ivLevel.IV == iv && ivLevel.Level >= level)
                        {
                            foundMatch = true;
                            rank = ivStat;
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
                var value = Convert.ToDouble(rank.CompetitionRank);
                var ivs = new List<PvpRank.IvWithCp>();
                var currentIV = rank.IVs.FirstOrDefault(iv => iv.IV == iv.IV);
                ivs = currentIV == null
                    ? new()
                    : new List<PvpRank.IvWithCp> { currentIV };

                var lastStat = lastRank.IVs.FirstOrDefault();
                var stat = ivs.FirstOrDefault();
                if (lastRank != null && lastStat.Level == stat.Level &&
                    lastRank!.CompetitionRank == competitionIndex + 1 &&
                    lastStat.IV.Attack == stat.IV.Attack && lastStat.IV.Defense == stat.IV.Defense &&
                    lastStat.IV.Stamina == stat.IV.Stamina)
                {
                    if (rankings.Exists(r => r.CompetitionRank == lastRank.CompetitionRank))
                    {
                        lastRank.IsCapped = true;
                        rankings.ForEach(r =>
                        {
                            if (r.CompetitionRank == lastRank.CompetitionRank)
                            {
                                r = lastRank;
                            }
                        });
                    }
                }
                else
                {
                    lastRank = new PvpRank
                    {
                        CompetitionRank = Convert.ToUInt32(competitionIndex + 1),
                        DenseRank = Convert.ToUInt32(denseIndex + 1),
                        OrdinalRank = Convert.ToUInt32(ordinalIndex + 1),
                        Percentage = value / max,
                        Cap = rank.Cap,
                        IsCapped = false,
                        IVs = ivs,
                    };
                    rankings.Add(lastRank);
                }
            }
            return rankings;
        }

        public Dictionary<string, dynamic>? GetAllPvpLeagues(HoloPokemonId pokemon, PokemonForm form, PokemonGender gender, PokemonCostume costume, IV iv, double level)
        {
            var pvp = new Dictionary<string, dynamic>();
            foreach (var leagueId in Enum.GetValues(typeof(PvpLeague)))
            {
                var league = (PvpLeague)leagueId;
                var leagueName = league.ToString().ToLower();
                var stats = GetPvpStatsWithEvolutions(pokemon, form, gender, costume, iv, level, league)?
                    .Select(ranking =>
                    {
                        var rank = 0u;
                        var (pokemon, response) = ranking;
                        switch (Strings.DefaultRank)
                        {
                            case RankType.Dense:
                                rank = response.DenseRank;
                                break;
                            case RankType.Competition:
                                rank = response.CompetitionRank;
                                break;
                            case RankType.Ordinal:
                                rank = response.OrdinalRank;
                                break;
                        }

                        var firstIV = response.IVs.FirstOrDefault();
                        var json = new
                        {
                            pokemon = pokemon.Pokemon,
                            form = pokemon.Form,
                            gender = pokemon.Gender,
                            rank = ranking,
                            percentage = response.Percentage,
                            cp = firstIV.CP,
                            level = firstIV.Level,
                            competition_rank = response.CompetitionRank,
                            dense_rank = response.DenseRank,
                            ordinal_rank = response.OrdinalRank,
                            cap = response.Cap,
                            capped = response.IsCapped,
                        };
                        return json;
                    }).ToList();
                if ((stats?.Count ?? 0) > 0)
                {
                    pvp[leagueName] = stats;
                }
            }
            return pvp.Count == 0
                ? null
                : pvp;
        }

        public List<PvpRank> GetTopPvpRanks(HoloPokemonId pokemon, PokemonForm form, PvpLeague league)
        {
            var info = new PokemonWithFormAndGender(pokemon, form);
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
                switch (league)
                {
                    case PvpLeague.Little:
                        break;
                    case PvpLeague.Great:
                        break;
                    case PvpLeague.Ultra:
                        break;
                }

                var stats = _stats.ContainsKey(info)
                    ? _stats[info]
                    : null;
                if (stats == null)
                {
                    return null;
                }

                /*
                let event = Threading.Event()
                switch league {
                case .little:
                    rankingLittle[info] = .event(event: event)
                    rankingLittleLock.unlock()
                case .great:
                    rankingGreat[info] = .event(event: event)
                    rankingGreatLock.unlock()
                case .ultra:
                    rankingUltra[info] = .event(event: event)
                    rankingUltraLock.unlock()
                }
                 */
                var values = CalculateAllRanks(stats, (ushort)league);
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

            var top = GetTopPvpRanks(pokemon, form, league);
            return top;
        }

        #endregion

        #region Private Methods

        private List<(PokemonWithFormAndGender, PvpRank)> GetPvpStatsWithEvolutions(HoloPokemonId pokemon, PokemonForm form, PokemonGender gender, PokemonCostume costume, IV iv, double level, PvpLeague league)
        {
            var rankings = GetPvpStats(pokemon, form, iv, level, league);
            var result = rankings.Select(rank => (new PokemonWithFormAndGender(pokemon, form, gender), rank))
                              .ToList();

            var pkmn = new PokemonWithFormAndGender(pokemon, form);
            // TODO: Fix
            if (!_stats.ContainsKey(pkmn))
            {
                return null;
            }

            var stat = _stats[pkmn];
            var hasNoEvolveForm = costume.ToString().ToLower().Contains(Strings.NoEvolveForm);
            var hasCostumeEvoOverride = stat.CostumeEvolutionOverride != null &&
                stat.CostumeEvolutionOverride.Count > 0 &&
                stat.CostumeEvolutionOverride.Contains(costume);

            if (stat != null &&
                (stat?.Evolutions?.Count ?? 0) > 0 &&
                (!hasNoEvolveForm || hasCostumeEvoOverride))
            {
                return result;
            }

            if (stat != null)
            {
                foreach (var evolution in stat.Evolutions)
                {
                    if (evolution.Gender == null || evolution.Gender == gender)
                    {
                        var pvpStats = GetPvpStatsWithEvolutions(evolution.Pokemon, evolution.Form ?? PokemonForm.Unset, gender, costume, iv, level, league);
                        result.AddRange(pvpStats);
                    }
                }
            }
            return result;
        }

        private static List<PvpRank> CalculateAllRanks(PokemonBaseStats stats, ushort cpCap)
        {
            var rankings = new List<PvpRank>();
            foreach (var levelCap in Strings.LevelCaps)
            {
                var cp = CalculateCP(stats, IV.GetHundoCombination(), levelCap);
                if (cp <= Strings.LeagueFilters[cpCap])
                    continue;

                var pvpStats = CalculatePvpStat(stats, cpCap, levelCap);
                //var keys = pvpStats.Keys.ToList();
                //keys.Sort((a, b) => a.CompareTo(b));
                //var sortedPvpStats = keys.ToDictionary<uint, PvpRank>((key, value) => key, value => pvpStat[x]);
                // TODO: Convert to list, sort, then convert back to dictionary
                //pvpStats.Sort((a, b) => a.Key.CompareTo(b.Key));
                /*
                    pvpStats.sorted { (lhs, rhs) -> Bool in
                        lhs.key >= rhs.key }
                            .map { (value) -> Response in
                        value.value }
                 */
                rankings.AddRange(pvpStats.Values);
            }
            return rankings;
        }

        private static SortedDictionary<uint, PvpRank> CalculatePvpStat(PokemonBaseStats stats, ushort cpCap, ushort levelCap)
        {
            var ranking = new SortedDictionary<uint, PvpRank>(new PvpRankComparer());
            var allCombinations = IV.GetAllCombinations();
            foreach (var iv in allCombinations)
            {
                double lowest = 1.0, highest = Convert.ToDouble(levelCap);
                uint bestCP = 0;
                while (lowest < highest)
                {
                    var mid = Math.Ceiling(lowest + highest) / 2;
                    var cp = CalculateCP(stats, iv, mid);
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
                if (lowest != 0)
                {
                    var value = CalculateStatProduct(stats, iv, lowest);
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

                    // TODO: ContainsKey
                    var ivWithCp = new PvpRank.IvWithCp(iv, lowest, bestCP);
                    //if let index = ranking[value]!.ivs.firstIndex(where: { bestCP >= $0.cp }) {
                    if (ranking[value].IVs.Exists(iv => bestCP >= iv.CP))
                    {
                        ranking[value].IVs.Insert(/*TODO: Get index*/ 0, ivWithCp);
                    }
                    else
                    {
                        ranking[value].IVs.Add(ivWithCp);
                    }
                }
            }
            return ranking;
        }

        private static uint CalculateStatProduct(PokemonBaseStats stats, IV iv, double level)
        {
            var multiplier = Strings.CpMultipliers[level];
            var hp = Math.Floor(Convert.ToDouble(iv.Stamina + stats.BaseStamina) * multiplier);
            hp = hp < 10 ? 10 : hp;
            var attack = Convert.ToDouble(iv.Attack + stats.BaseAttack) * multiplier;
            var defense = Convert.ToDouble(iv.Defense + stats.BaseDefense) * multiplier;
            var product = Convert.ToUInt32(Math.Round(attack * defense * hp));
            return product;
        }

        private static uint CalculateCP(PokemonBaseStats stats, IV iv, double level)
        {
            var attack = Convert.ToDouble(stats.BaseAttack + iv.Attack);
            var defense = Math.Pow(Convert.ToDouble(stats.BaseDefense + iv.Defense), 0.5);
            var stamina = Math.Pow(Convert.ToDouble(stats.BaseStamina + iv.Stamina), 0.5);
            var multiplier = Math.Pow(Strings.CpMultipliers[level], 2);
            var cp = Math.Max(Convert.ToUInt32(Math.Floor(attack * defense * stamina * multiplier / 10)), 10);
            return cp;
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

            var stats = new Dictionary<PokemonWithFormAndGender, PokemonBaseStats>();
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

                    var pokemonName = pokemonInfo.PokemonId;
                    var baseStats = pokemonInfo.Stats;
                    var pokedexHeightM = pokemonInfo.PokedexHeightM;
                    var pokedexWeightKg = pokemonInfo.PokedexWeightKg;
                    var baseAttack = baseStats.BaseAttack;
                    var baseDefense = baseStats.BaseDefense;
                    var baseStamina = baseStats.BaseStamina;
                    var pokemon = PokemonFromName(pokemonName);
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
                        var formId = FormFromName(formName);
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
                            var evoPokemon = PokemonFromName(evoName);
                            if (!string.IsNullOrEmpty(evoName) && evoPokemon != HoloPokemonId.Missingno)
                            {
                                var evoFormName = info.Form;
                                var genderName = info.GenderRequirement;
                                PokemonForm? evoForm = string.IsNullOrEmpty(evoFormName)
                                    ? null
                                    : FormFromName(evoFormName);
                                PokemonGender? evoGender = string.IsNullOrEmpty(genderName)
                                    ? null
                                    : GenderFromName(genderName);
                                evolutions.Add(new PokemonWithFormAndGender(evoPokemon, evoForm, evoGender));
                            }
                        }
                    }

                    var costumeEvolution = pokemonInfo.ObCostumeEvolution?
                        .Where(costume => CostumeFromName(costume) != PokemonCostume.Unset)
                        .Select(CostumeFromName)
                        .ToList();
                    var stat = new PokemonBaseStats
                    {
                        BaseAttack = baseAttack,
                        BaseDefense = baseDefense,
                        BaseStamina = baseStamina,
                        Evolutions = evolutions,
                        BaseHeight = pokedexHeightM,
                        BaseWeight = pokedexWeightKg,
                        CostumeEvolutionOverride = costumeEvolution,
                    };
                    stats[new PokemonWithFormAndGender(pokemon, form)] = stat;
                }
            }

            _stats = stats;
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
        private static PokemonForm FormFromName(string name)
        {
            var allForms = new List<PokemonForm>(Enum.GetValues<PokemonForm>());
            var form = GetEnumFromName(name, allForms);
            return form;
        }

        private static HoloPokemonId PokemonFromName(string name)
        {
            var allPokemon = new List<HoloPokemonId>(Enum.GetValues<HoloPokemonId>());
            var pokemon = GetEnumFromName(name, allPokemon);
            return pokemon;
        }

        private static PokemonGender GenderFromName(string name)
        {
            var allGenders = new List<PokemonGender>(Enum.GetValues<PokemonGender>());
            var gender = GetEnumFromName(name, allGenders);
            return gender;
        }

        private static PokemonCostume CostumeFromName(string name)
        {
            var allCostumes = new List<PokemonCostume>(Enum.GetValues<PokemonCostume>());
            var costume = GetEnumFromName(name, allCostumes);
            return costume;
        }

        private static T? GetEnumFromName<T>(string name, List<T> values)
        {
            var lower = name.Replace("_", "").ToLower();
            var result = values.FirstOrDefault(x => x.ToString().ToLower() == lower);
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

        private class PvpRankComparer : IComparer<uint>
        {
            public int Compare(uint x, uint y) => x.CompareTo(y);
        }
    }
}