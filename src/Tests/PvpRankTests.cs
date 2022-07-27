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

        [TestCase(2, 13, 14, HoloPokemonId.Bulbasaur, Form.BulbasaurNormal)]
        [TestCase(0, 14, 14, HoloPokemonId.Ralts, Form.RaltsNormal)]//, Gender.Male)]
        [TestCase(0, 13, 14, HoloPokemonId.Riolu)]
        public void Test_Base_Pass(int atk, int def, int sta, HoloPokemonId pokemon, Form? form = null, Gender? gender = null, Costume? costume = null)
        {
            var iv = new IV((ushort)atk, (ushort)def, (ushort)sta);
            var level = 1;

            var ranks = _pvp.GetAllPvpLeagues(
                pokemon,
                form,
                gender,
                costume,
                iv,
                level
            );
            Assert.That(ranks?.Count == 3, Is.True);
        }

        [TestCase(0, 13, 14, HoloPokemonId.Lucario)]
        public void Test_Evolution_Pass(int atk, int def, int sta, HoloPokemonId pokemon, Form? form = null, Gender? gender = null, Costume? costume = null)
        {
            var iv = new IV((ushort)atk, (ushort)def, (ushort)sta);
            var level = 1;

            var evolutions = _pvp.GetAllPvpLeagues(
                pokemon,
                form,
                gender,
                costume,
                iv,
                level
            );
            /*
            var evolutions = _pvp.GetPvpStatsWithEvolutions(
                pokemon,
                form,
                gender,
                costume,
                iv,
                level,
                PvpLeague.Ultra
            );
            */

            Assert.That(evolutions?.Count == 3, Is.True);
        }
    }
}