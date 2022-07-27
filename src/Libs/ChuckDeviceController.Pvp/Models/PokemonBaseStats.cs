namespace ChuckDeviceController.Pvp.Models
{
    using PokemonCostume = POGOProtos.Rpc.PokemonDisplayProto.Types.Costume;

    public class PokemonBaseStats : IEquatable<PokemonBaseStats>, IComparable<PokemonBaseStats>
    {
        public ushort BaseAttack { get; set; }

        public ushort BaseDefense { get; set; }

        public ushort BaseStamina { get; set; }

        public List<PokemonWithFormAndGender> Evolutions { get; set; } = new();

        public double BaseHeight { get; set; }

        public double BaseWeight { get; set; }

        public List<PokemonCostume>? CostumeEvolutionOverride { get; set; }

        public PokemonBaseStats()
        {
            BaseAttack = 0;
            BaseDefense = 0;
            BaseStamina = 0;
            BaseHeight = 0;
            BaseWeight = 0;
            Evolutions = new();
            CostumeEvolutionOverride = null;
        }

        public bool Equals(PokemonBaseStats? other)
        {
            if (other is null)
                return false;

            return BaseAttack == other.BaseAttack &&
                   BaseDefense == other.BaseDefense &&
                   BaseStamina == other.BaseStamina &&
                   BaseHeight == other.BaseHeight &&
                   BaseWeight == other.BaseWeight;
        }

        public int CompareTo(PokemonBaseStats? other)
        {
            if (other is null)
                return -1;

            var result = BaseAttack.CompareTo(other.BaseAttack);
            if (result != 0)
                return result;

            result = BaseDefense.CompareTo(other.BaseDefense);
            if (result != 0)
                return result;

            result = BaseStamina.CompareTo(other.BaseStamina);
            if (result != 0)
                return result;

            result = BaseHeight.CompareTo(other.BaseHeight);
            if (result != 0)
                return result;

            result = BaseWeight.CompareTo(other.BaseWeight);
            if (result != 0)
                return result;

            return 0;
        }

        public override bool Equals(object? obj)
        {
            var other = obj as PokemonBaseStats;
            return Equals(other);
        }

        public override int GetHashCode() => BaseAttack ^ BaseDefense ^ BaseStamina;

        public static bool operator ==(PokemonBaseStats left, PokemonBaseStats right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(PokemonBaseStats left, PokemonBaseStats right)
        {
            return !(left == right);
        }

        public static bool operator <(PokemonBaseStats left, PokemonBaseStats right)
        {
            return left is null
                ? right is not null
                : left.CompareTo(right) < 0;
        }

        public static bool operator <=(PokemonBaseStats left, PokemonBaseStats right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(PokemonBaseStats left, PokemonBaseStats right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(PokemonBaseStats left, PokemonBaseStats right)
        {
            return left is null
                ? right is null
                : left.CompareTo(right) >= 0;
        }
    }
}