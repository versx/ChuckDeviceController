namespace ChuckDeviceController.Pvp.Extensions
{
    using ChuckDeviceController.Pvp.Models;

    public static class PokemonBaseStatsExtensions
    {
        public static uint CalculateStatProduct(this PokemonBaseStats baseStats, IV iv, double level)
        {
            var multiplier = Strings.CpMultipliers[level];
            var hp = Math.Floor(Convert.ToDouble(iv.Stamina + baseStats.BaseStamina) * multiplier);
            hp = hp < 10 ? 10 : hp;
            var attack = Convert.ToDouble(iv.Attack + baseStats.BaseAttack) * multiplier;
            var defense = Convert.ToDouble(iv.Defense + baseStats.BaseDefense) * multiplier;
            var product = Convert.ToUInt32(Math.Round(attack * defense * hp));
            return product;
        }

        public static uint CalculateCP(this PokemonBaseStats baseStats, IV iv, double level)
        {
            var attack = Convert.ToDouble(baseStats.BaseAttack + iv.Attack);
            var defense = Math.Pow(Convert.ToDouble(baseStats.BaseDefense + iv.Defense), 0.5);
            var stamina = Math.Pow(Convert.ToDouble(baseStats.BaseStamina + iv.Stamina), 0.5);
            var multiplier = Math.Pow(Strings.CpMultipliers[level], 2);
            var cp = Math.Max(Convert.ToUInt32(Math.Floor(attack * defense * stamina * multiplier / 10)), 10);
            return cp;
        }
    }
}