namespace Tests;

using System.Diagnostics;

using ChuckDeviceController.Collections;

[TestFixture]
internal class CollectionTests
{
    private readonly SafeCollection<int> _collection = new();

    [SetUp]
    public void SetUp()
    {
        for (var i = 0; i < 100000; i++)
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
        Assert.That(count, Is.EqualTo(itemsCount));
    }

    [TestCase(150)]
    public void TestTryTakeCollection(int count)
    {
        var items = _collection.TryTake(count);
        var itemsCount = items.Count();
        Assert.That(count, Is.EqualTo(itemsCount));
    }

    [Test]
    public void TestRemoveCollection()
    {
        var result = _collection.Remove(x => x == 3);
        Assert.That(result, Is.True);
    }

    [Test]
    public void MeasureBenchmarks()
    {
        var maxBatchSize = 10000;
        var result = new List<int>();
        var sw = new Stopwatch();
        sw.Start();

        for (var i = 0; i < maxBatchSize; i++)
        {
            if (!_collection.Any())
                break;

            if (!_collection.TryTake(out int item))
            {
                Console.WriteLine($"Failed to dequeue item: {item}");
                continue;
            }

            result.Add(item);
        }
        sw.Stop();
        var totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
        sw.Reset();
        Console.WriteLine($"Loop took: {totalSeconds}s");

        sw.Start();
        var results = _collection.Take(maxBatchSize);
        Console.WriteLine($"Results: {results}");
        sw.Stop();
        totalSeconds = Math.Round(sw.Elapsed.TotalSeconds, 4);
        Console.WriteLine($"Take took: {totalSeconds}s");
    }
}
