namespace ChuckDeviceController.Pvp.Models
{
    using POGOProtos.Rpc;
    using PokemonForm = POGOProtos.Rpc.PokemonDisplayProto.Types.Form;
    using PokemonGender = POGOProtos.Rpc.PokemonDisplayProto.Types.Gender;

    public class PokemonWithFormAndGender : IComparable
    {
        public HoloPokemonId Pokemon { get; set; }

        public PokemonForm? Form { get; set; }// = PokemonForm.Unset;

        public PokemonGender? Gender { get; set; }

        public PokemonWithFormAndGender(HoloPokemonId pokemon, PokemonForm? form)
        {
            Pokemon = pokemon;
            Form = form;
        }

        public PokemonWithFormAndGender(HoloPokemonId pokemon, PokemonForm? form, PokemonGender? gender)
        {
            Pokemon = pokemon;
            Form = form;
            Gender = gender;
        }

        public int CompareTo(object? obj)
        {
            if (obj == null)
                return -1;

            var other = (PokemonWithFormAndGender)obj;
            var result = Pokemon.CompareTo(other.Pokemon);
            if (result != 0)
            {
                return result;
            }

            result = (Form ?? PokemonForm.Unset).CompareTo(other.Form ?? PokemonForm.Unset);
            if (result != 0)
            {
                return result;
            }

            result = (Gender ?? PokemonGender.Unset).CompareTo(other.Gender ?? PokemonGender.Unset);
            if (result != 0)
            {
                return result;
            }

            return 0;
        }
    }
}