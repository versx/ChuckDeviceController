namespace Tests
{
    using POGOProtos.Rpc;
    using static POGOProtos.Rpc.PokemonDisplayProto.Types;

    using ChuckDeviceController.Pvp;
    using ChuckDeviceController.Pvp.Models;

    public class Tests
    {
        private readonly PvpRankGenerator _pvp = new();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_IV_Comparison_Pass()
        {
            var iv = new IV(0, 15, 15);
            var iv2 = new IV(0, 15, 15);
            var result = iv == iv2;
            Assert.That(result, Is.True);
        }

        [Test]
        public void Test_IV_Comparison_Fail()
        {
            var iv = new IV(0, 15, 15);
            var iv2 = new IV(0, 14, 15);
            var result = iv == iv2;
            Assert.That(result, Is.False);
        }

        [Test]
        public void Test_Pokemon_Comparison_Pass()
        {
            var pkmn = new PokemonWithFormAndGender { Pokemon = HoloPokemonId.Bulbasaur, Form = Form.BulbasaurNormal };
            var pkmn2 = new PokemonWithFormAndGender { Pokemon = HoloPokemonId.Bulbasaur, Form = Form.BulbasaurNormal };
            var result = pkmn == pkmn2;
            Assert.That(result, Is.True);
        }

        [Test]
        public void Test_Pokemon_Comparison_Fail()
        {
            var pkmn = new PokemonWithFormAndGender { Pokemon = HoloPokemonId.Bulbasaur, Form = Form.BulbasaurNormal };
            var pkmn2 = new PokemonWithFormAndGender { Pokemon = HoloPokemonId.Bulbasaur };//, Form = Form.BulbasaurNormal, Gender = Gender.Male };
            var result = pkmn == pkmn2;
            Assert.That(result, Is.False);
        }

        [Test]
        public void Test1()
        {
            var pokemon = HoloPokemonId.Lucario;
            Form? form = null;//Form.RioluNormal;
            Gender? gender = null;//Gender.Male;
            var costume = Costume.Unset;
            var iv = new IV(0, 14, 14);
            //var iv = new IV(0, 14, 15);
            //var iv = new IV(3, 13, 13);
            var level = 1;
            var league = PvpLeague.Ultra;

            var bulba = _pvp.GetAllPvpLeagues(
                pokemon,
                form,
                gender,
                costume,
                iv,
                level
            );
            Console.WriteLine($"Ranks: {bulba}");

            var bulbaEvos = _pvp.GetPvpStatsWithEvolutions(
                pokemon,
                form,
                gender,
                costume,
                iv,
                level,
                league
            );

            Console.WriteLine($"Ranks Evolutions: {bulbaEvos}");
        }
    }
}