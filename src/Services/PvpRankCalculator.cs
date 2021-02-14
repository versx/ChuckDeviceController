﻿namespace ChuckDeviceController.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Data;
    using ChuckDeviceController.Data.Entities;

    /// <summary>
    /// 
    /// </summary>
    /// <credits>
    /// https://github.com/WatWowMap/Chuck/blob/master/src/services/pvp.js
    /// https://github.com/WatWowMap/Chuck/blob/master/src/services/pvp-core.js
    /// </credits>
    public class PvpRankCalculator
    {
        // TODO: Cache generated stats

        private static readonly List<(string, ushort)> _availableLeagues = new List<(string, ushort)>
        {
            ("great", 1500),
            ("ultra", 2500),
        };
        private static readonly List<ushort> _availableLevelCaps = new List<ushort>
        {
            40,
            41,
            50,
            51,
        };

        #region Singleton

        private static PvpRankCalculator _instance;
        public static PvpRankCalculator Instance => _instance ??= new PvpRankCalculator();

        #endregion

        #region Public Methods

        public Dictionary<string, List<PvpRank>> QueryPvpRank(uint pokemonId, uint formId, uint costumeId, ushort atk, ushort def, ushort sta, double level, PokemonGender gender)
        {
            if (!GameMaster.Instance.Pokedex.ContainsKey(pokemonId))
            {
                // Pokemon not found in gamemaster
                return null;
            }
            var masterPokemon = GameMaster.Instance.Pokedex[pokemonId];
            if (!masterPokemon.Attack.HasValue)
            {
                // No base attack specified
                return null;
            }
            var masterForm = formId > 0 ? masterPokemon.Forms[formId] ?? masterPokemon : masterPokemon;
            var baseEntry = new PvpRank();
            baseEntry.Pokemon = (ushort)pokemonId;
            if (formId > 0)
            {
                baseEntry.Form = (ushort)formId;
            }
            var results = new Dictionary<string, List<PvpRank>>();

            Action<PokedexPokemon, ushort> pushAllEntries = (PokedexPokemon stats, ushort evolution) =>
            {
                Dictionary<string, Dictionary<ushort, StatCombination>> allRanks = CalculateAllRanks(stats);
                foreach ((string leagueName, Dictionary<ushort, StatCombination> combinationIndex) in allRanks)
                {
                    foreach ((ushort levelCap, StatCombination combinations) in combinationIndex)
                    {
                        var ivEntry = combinations[atk][def][sta];
                        if (level > ivEntry.Level)
                        {
                            continue;
                        }
                        var entry = new PvpRank
                        {
                            Pokemon = baseEntry.Pokemon,
                            Form = baseEntry.Form,
                            LevelCap = levelCap,
                            Value = ivEntry.Value,
                            CP = ivEntry.CP,
                            Level = ivEntry.Level,
                            Percentage = ivEntry.Percentage,
                            Rank = ivEntry.Rank,
                        };
                        if (evolution > 0)
                        {
                            entry.Evolution = evolution;
                        }
                        /*
                        if (combinations.Maxed)
                        {
                            entry.IsCapped = true;
                        }
                        */
                        if (!results.ContainsKey(leagueName))
                        {
                            results.Add(leagueName, new List<PvpRank>());
                        }
                        results[leagueName].Add(entry);
                    }
                }
            };
            pushAllEntries(masterForm.Attack.HasValue ? masterForm : masterPokemon, 0);
            var canEvolve = true;
            if (costumeId > 0)
            {
                var costumeName = ""; // TODO: Get Pokemon costume name from protos
                canEvolve = costumeName.EndsWith("_NOEVOLVE") || costumeName.EndsWith("_NO_EVOLVE");
            }
            if (canEvolve && masterForm.Evolutions.Count > 0)
            {
                foreach (var evolution in masterForm.Evolutions)
                {
                    if (evolution.GenderRequirement > 0 && gender != evolution.GenderRequirement)
                    {
                        continue;
                    }
                    // Eventually remove this after masterfile is fixed
                    if (evolution.PokedexId == 0)
                        evolution.PokedexId = evolution.Pokemon;
                    if (evolution.DefaultFormId == 0)
                        evolution.DefaultFormId = (int)evolution.FormId;

                    // Reset costume since we know it can't evolve
                    var evolvedRanks = QueryPvpRank(evolution.PokedexId, Convert.ToUInt32(evolution.DefaultFormId ?? 0), 0, atk, def, sta, level, gender);
                    foreach (var (leagueName, result) in evolvedRanks)
                    {
                        if (results.ContainsKey(leagueName))
                        {
                            results[leagueName].AddRange(result);
                        }
                        else
                        {
                            results.Add(leagueName, result);
                        }
                    }
                }
            }
            if (masterForm.TempEvolutions.Count > 0)
            {
                foreach (var (tempEvoId, tempEvo) in masterForm.TempEvolutions)
                {
                    pushAllEntries(tempEvo.Attack.HasValue ? tempEvo : masterPokemon.TempEvolutions[tempEvoId], (ushort)tempEvoId);
                }
            }
            return results;
        }

        #endregion

        #region Private Methods

        private PvpRank CalculatePvPStat(PokedexPokemon stats, ushort atk, ushort def, ushort sta, ushort cpCap, double levelCap)
        {
            var bestCP = cpCap;
            double lowest = 1;
            var highest = levelCap;
            for (var mid = Math.Ceiling(lowest + highest) / 2; lowest < highest; mid = Math.Ceiling(lowest + highest) / 2)
            {
                var cp = CalculateCP(stats, atk, def, sta, mid);
                if (cp <= cpCap)
                {
                    lowest = mid;
                    bestCP = (ushort)cp;
                }
                else
                {
                    highest = mid - .5;
                }
            }
            return new PvpRank
            {
                Value = (uint)CalculateStatProduct(stats, atk, def, sta, lowest),
                Level = lowest,
                CP = bestCP,
            };
        }

        private (StatCombination, List<PvpRank>) CalculateRanks(PokedexPokemon stats, ushort cpCap, double levelCap)
        {
            var combinations = new StatCombination();
            var sortedRanks = new List<PvpRank>();
            for (ushort a = 0; a <= 15; a++)
            {
                var atkStats = new List<List<PvpRank>>();
                for (ushort d = 0; d <= 15; d++)
                {
                    var defStats = new List<PvpRank>();
                    for (ushort s = 0; s <= 15; s++)
                    {
                        var currentStat = CalculatePvPStat(stats, a, d, s, cpCap, levelCap);
                        defStats.Add(currentStat);
                        sortedRanks.Add(currentStat);
                    }
                    atkStats.Add(defStats);
                }
                combinations.Add(atkStats);
            }
            sortedRanks.Sort((a, b) => Convert.ToInt32(b.Value - a.Value));
            var best = sortedRanks.FirstOrDefault().Value;
            for (int i = 0, j = 0; i < sortedRanks.Count; i++)
            {
                var entry = sortedRanks[i];
                entry.Percentage = Math.Round((double)(entry.Value / best), 5);
                if (entry.Value < sortedRanks[j].Value)
                {
                    j = i;
                }
                entry.Rank = Convert.ToUInt16(j + 1);
            }
            return (combinations, sortedRanks);
        }

        private Dictionary<string, Dictionary<ushort, StatCombination>> CalculateAllRanks(PokedexPokemon stats)
        {
            //var key = $"{stats.attack},{stats.defense},{stats.stamina}";
            // TODO: Get key from cache
            var result = new Dictionary<string, Dictionary<ushort, StatCombination>>();
            foreach (var (leagueName, cpCap) in _availableLeagues)
            {
                var combinationIndex = new Dictionary<ushort, StatCombination>();
                foreach (var levelCap in _availableLevelCaps)
                {
                    if (CalculateCP(stats, 15, 15, 15, levelCap) <= cpCap)
                    {
                        continue; // Not viable cp
                    }
                    (StatCombination, List<PvpRank>) rank = CalculateRanks(stats, cpCap, levelCap);
                    if (combinationIndex.ContainsKey(levelCap))
                        combinationIndex[levelCap] = rank.Item1;
                    else
                        combinationIndex.Add(levelCap, rank.Item1);
                    if (CalculateCP(stats, 0, 0, 0, levelCap + 0.5) > cpCap)
                    {
                        // TODO: rank.Item1.Maxed = true;
                        break;
                    }
                }
                if (result.ContainsKey(leagueName))
                    result[leagueName] = combinationIndex;
                else
                    result.Add(leagueName, combinationIndex);
            }
            // Set cache
            return result;
        }

        #endregion

        #region Static Methods

        public static double CalculateStatProduct(PokedexPokemon stats, ushort atk, ushort def, ushort sta, double level)
        {
            var multiplier = GameMaster.Instance.CpMultipliers[level];
            var hp = Math.Floor((sta + stats.Stamina ?? 0) * multiplier);
            if (hp < 10) hp = 10;
            return (atk + stats.Attack ?? 0) * multiplier *
                   (def + stats.Defense ?? 0) * multiplier *
                   hp;
        }

        public static uint CalculateCP(PokedexPokemon stats, ushort atk, ushort def, ushort sta, double level)
        {
            var multiplier = GameMaster.Instance.CpMultipliers[level];
            var attack = (double)(stats.Attack + atk);
            var defense = (double)(stats.Defense + def);
            var stamina = (double)(stats.Stamina + sta);
            var cp = Math.Floor(multiplier * multiplier * attack * Math.Sqrt(defense * stamina) / 10);
            return Convert.ToUInt32(cp < 10 ? 10 : cp);
        }

        #endregion
    }

    public class StatCombination : List<List<List<PvpRank>>> { }
}