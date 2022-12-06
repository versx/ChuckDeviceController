namespace Tests
{
    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Data.Entities;

    [TestFixture]
    internal class PriorityQueueTests
    {
        private const ushort MaxQueueSize = 100;

        private readonly SortedPriorityQueue<Pokemon> _queue;
        private readonly PokemonComparer _comparer;
        private readonly List<string> _priorityList;

        public PriorityQueueTests()
        {
            _priorityList = new List<string>
            {
                "9",
                "8",
                "7",
                "6",
                "5",
                "4",
                "3",
                "2",
                "1",
                "0",
            };
            _comparer = new PokemonComparer(_priorityList);
            _queue = new SortedPriorityQueue<Pokemon>(MaxQueueSize, _comparer);
        }

        [SetUp]
        public void SetUp()
        {
            //for (var i = 0; i < 100; i++)
            //{
            //    var pokemon = new Pokemon
            //    {
            //        PokemonId = (uint)i,
            //        //Form = 0,
            //    };
            //    //_queue.Enqueue(pokemon, pokemon);
            //    _queue.Add(pokemon);
            //}
            //var ids = new[] { "3", "9", "1", "5" };
            //foreach (var id in ids)
            //{
            //    var pokemon = new Pokemon
            //    {
            //        PokemonId = uint.Parse(id),
            //        //Form = 0,
            //    };
            //    //_queue.Enqueue(pokemon, pokemon);
            //    _queue.Add(pokemon);
            //}
        }

        [Test]
        public void TestQueueOrdering()
        {
            var pokemon = new Pokemon
            {
                PokemonId = 25,
                //Form = 0,
            };
            _queue.Add(pokemon);
            Assert.That(_queue.Count == 5, Is.True);
        }

        [Test]
        public void TestDequeue()
        {
            var pokemon = new Pokemon
            {
                PokemonId = 25,
                //Form = 0,
            };
            _queue.Add(pokemon);
            var item = _queue.Dequeue();
            Assert.That(item.PokemonId == 9, Is.True);
            Assert.That(_queue.Count == 4, Is.True);
        }

        [Test]
        public void TestDequeueLast()
        {
            var pokemon = new Pokemon
            {
                PokemonId = 25,
                //Form = 0,
            };
            _queue.Add(pokemon);
            var item = _queue.DequeueLast();
            Assert.That(item.PokemonId == 25, Is.True);
            Assert.That(_queue.Count == 4, Is.True);
        }

        [TestCase(150)]
        public void TestQueueLimit(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var pokemon = new Pokemon
                {
                    PokemonId = (uint)i,
                    //Form = 0,
                };
                _queue.Add(pokemon);
            }

            var pkmn = new Pokemon
            {
                PokemonId = 150,
                Form = 0,
            };
            _queue.Add(pkmn);

            Assert.That(_queue.Count == MaxQueueSize, Is.True);
        }
    }

    public class PokemonComparer : IComparer<Pokemon>
    {
        private readonly List<string> _priorityList;

        public PokemonComparer(List<string> priorityList)
        {
            _priorityList = priorityList;
        }

        public int Compare(Pokemon? x, Pokemon? y)
        {
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return -1;
            }

            var index1 = GetPriorityIndex(x);
            var index2 = GetPriorityIndex(y);
            var result = index1.CompareTo(index2);
            return result;
        }

        private int GetPriorityIndex(Pokemon pokemon)
        {
            var key = pokemon.Form > 0
                ? $"{pokemon.PokemonId}_f{pokemon.Form}"
                : $"{pokemon.PokemonId}";
            var priority = _priorityList.IndexOf(key);
            if (priority > -1)
            {
                return priority;
            }
            if (pokemon.Form > 0)
            {
                var index = _priorityList.IndexOf($"{pokemon.PokemonId}");
                return index;
            }
            // Return default index to order item at the end of the queue
            return int.MaxValue;
        }
    }
}