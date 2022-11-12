namespace ChuckDeviceController.Pvp.Models
{
    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

    public struct PokemonWithFormAndGender : IEquatable<PokemonWithFormAndGender>, IComparable<PokemonWithFormAndGender>
    {
        public HoloPokemonId Pokemon { get; set; }

        public PokemonForm? Form { get; set; }

        public PokemonGender? Gender { get; set; }

        public int CompareTo(PokemonWithFormAndGender other)
        {
            var result = Pokemon.CompareTo(other.Pokemon);
            if (result != 0)
            {
                return result;
            }

            //result = (Form ?? PokemonForm.Unset).CompareTo(other.Form ?? PokemonForm.Unset);
            result = (Form == null && other.Form == null)
                ? 0
                : (Form ?? PokemonForm.Unset).CompareTo(other.Form ?? PokemonForm.Unset);
            if (result != 0)
            {
                return result;
            }

            //result = (Gender ?? PokemonGender.Unset).CompareTo(other.Gender ?? PokemonGender.Unset);
            result = (Gender == null && other.Gender == null)
                ? 0
                : (Gender ?? PokemonGender.Unset).CompareTo(other.Gender ?? PokemonGender.Unset);
            if (result != 0)
            {
                return result;
            }

            return 0;
        }

        public bool Equals(PokemonWithFormAndGender other)
        {
            return Pokemon == other.Pokemon &&
                   (Form == other.Form || (Form == null && other.Form == null)) &&
                   (Gender == other.Gender || (Gender == null && other.Gender == null));
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            var other = (PokemonWithFormAndGender)obj;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(Pokemon) ^
                   Convert.ToInt32(Form ?? 0) ^
                   Convert.ToInt32(Gender ?? 0);
        }

        public static bool operator ==(PokemonWithFormAndGender left, PokemonWithFormAndGender right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PokemonWithFormAndGender left, PokemonWithFormAndGender right)
        {
            return !(left == right);
        }

        public static bool operator <(PokemonWithFormAndGender left, PokemonWithFormAndGender right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(PokemonWithFormAndGender left, PokemonWithFormAndGender right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(PokemonWithFormAndGender left, PokemonWithFormAndGender right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(PokemonWithFormAndGender left, PokemonWithFormAndGender right)
        {
            return left.CompareTo(right) >= 0;
        }

        public override string ToString()
        {
            return $"{Pokemon}_{Form}_{Gender}";
        }
    }
}