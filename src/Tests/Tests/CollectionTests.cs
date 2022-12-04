namespace Tests
{
    using ChuckDeviceController.Collections;

    [TestFixture]
    internal class CollectionTests
    {
        private readonly SafeCollection<int> _collection = new();

        [SetUp]
        public void SetUp()
        {
            for (var i = 0; i < 1000; i++)
            {
                if (!_collection.TryAdd(i))
                {
                    Console.WriteLine($"Failed to add item '{i}'");
                }
            }
        }

        [TestCase(150)]
        public void TestTakeCollection(int count)
        {
            var items = _collection.Take(count);
            var itemsCount = items.Count();
            Assert.That(count == itemsCount, Is.True);
        }

        [TestCase(150)]
        public void TestTryTakeCollection(int count)
        {
            var items = _collection.TryTake(count);
            var itemsCount = items.Count();
            Assert.That(count == itemsCount, Is.True);
        }

        [Test]
        public void TestRemoveCollection()
        {
            var result = _collection.Remove(x => x == 3);
            Assert.That(result, Is.True);
        }
    }
}
