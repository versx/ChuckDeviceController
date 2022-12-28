namespace Dapper.Tests;

using System.Diagnostics;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Extensions;

internal class RepositoryTests
{
    private const string ConnectionString = "";

    [SetUp]
    public void Setup()
    {
        EntityDataRepository.SetTypeMap<Cell>();
        EntityDataRepository.SetTypeMap<Pokestop>();
    }

    [Test]
    public async Task TestSelectCell()
    {
        var cellRepo = new CellRepository("s2cell", ConnectionString);
        var cell = await cellRepo.FindAsync(123);

        Assert.That(cell, Is.Not.Null);
    }

    [Test]
    public async Task TestInsertCell()
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var sw = new Stopwatch();

        sw.Start();
        var repo = new CellRepository("s2cell", ConnectionString);
        var affectedRows = await repo.InsertAsync(new Cell
        {
            Id = 123,
            Latitude = 34.01,
            Longitude = -117.01,
            Level = 15,
            Updated = now,
        });
        sw.Stop();
        var time = Math.Round(sw.Elapsed.TotalSeconds, 4);
        Console.WriteLine($"Time taken: {time}s");

        Assert.That(affectedRows, Is.EqualTo(1));
    }

    [Test]
    public async Task TestInsertRangeCells()
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var repo = new CellRepository("s2cell", ConnectionString);
        var cells = new List<Cell>
        {
            new Cell
            {
                Id = 123,
                Latitude = 34.01,
                Longitude = -117.01,
                Level = 15,
                Updated = now,
            },
            new Cell
            {
                Id = 1234,
                Latitude = 34.01,
                Longitude = -117.01,
                Level = 15,
                Updated = now,
            },
            new Cell
            {
                Id = 12345,
                Latitude = 34.01,
                Longitude = -117.01,
                Level = 15,
                Updated = now,
            },
            new Cell
            {
                Id = 123456,
                Latitude = 34.01,
                Longitude = -117.01,
                Level = 15,
                Updated = now,
            },
        };

        var affectedRows = await repo.InsertRangeAsync(cells);
        Assert.That(affectedRows, Is.EqualTo(4));
    }

    [Test]
    public async Task TestInsertPokestop()
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var sw = new Stopwatch();

        sw.Start();
        var stopRepo = new PokestopRepository("pokestop", ConnectionString);
        var affectedRows = await stopRepo.InsertAsync(new Pokestop
        {
            Id = "123_test",
            Latitude = 34.01,
            Longitude = -117.01,
            Name = "Test",
            Url = "https://ver.sx",
            CellId = 123,
            FirstSeenTimestamp = now,
            Updated = now,
            LastModifiedTimestamp = now,
            LureId = 501,
            LureExpireTimestamp = now,
            IsEnabled = true,
            IsDeleted = true,
            IsArScanEligible = true,
        });
        sw.Stop();
        var time = Math.Round(sw.Elapsed.TotalSeconds, 4);
        Console.WriteLine($"Time taken: {time}s");

        Assert.That(affectedRows, Is.EqualTo(1));
    }

    [Test]
    public async Task TestUpdateCell()
    {
        var repo = new CellRepository("s2cell", ConnectionString);
        var cell = await repo.FindAsync(123);
        cell.Latitude = 34.04;
        cell.Longitude = -117.04;
        cell.Updated = DateTime.UtcNow.ToTotalSeconds();
        var affectedRows = await repo.UpdateAsync(cell);

        Assert.That(affectedRows, Is.EqualTo(1));
    }

    [Test]
    public async Task TestUpdatePokestopWithMappings()
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var timeTaken = BenchmarkAction(async () =>
        {
            var stopRepo = new PokestopRepository("pokestop", ConnectionString);
            var pokestop = await stopRepo.FindAsync("123_test");

            var affectedRows = await stopRepo.UpdateAsync(pokestop, new Dictionary<string, Func<Pokestop, object>>()
            {
                ["updated"] = x => now,
                ["power_up_level"] = x => 15,
                ["power_up_end_timestamp"] = x => now,
                ["power_up_points"] = x => 250,
                ["lure_expire_timestamp"] = x => now,
                ["lure_id"] = x => 505,
                ["quest_template"] = x => "template_kljlkjfskdf",
                ["quest_timestamp"] = x => now,
                ["quest_target"] = x => 1,
                ["quest_type"] = x => 5,
                ["quest_title"] = x => "Test quest title!",
            });
        });
        Console.WriteLine($"Time taken: {timeTaken}s");

        await Task.CompletedTask;
        Assert.That(true, Is.True);
    }

    [Test]
    public async Task TestUpdatePokestopWithAllProperties()
    {
        var now = DateTime.UtcNow.ToTotalSeconds();
        var timeTaken = BenchmarkAction(async () =>
        {
            var stopRepo = new PokestopRepository("pokestop", ConnectionString);
            var pokestop = await stopRepo.FindAsync("123_test");
            pokestop.PowerUpLevel = 10;
            pokestop.PowerUpEndTimestamp = now;
            pokestop.PowerUpPoints = 150;
            pokestop.LureExpireTimestamp = now;
            pokestop.LureId = 501;
            pokestop.QuestTemplate = "hlksdjf3j";
            pokestop.QuestTimestamp = now;
            pokestop.QuestTarget = 3;
            pokestop.QuestType = 2;
            pokestop.QuestTitle = "Test quest title";

            var affectedRows = await stopRepo.UpdateAsync(pokestop);
        });
        Console.WriteLine($"Time taken: {timeTaken}s");

        await Task.CompletedTask;
        Assert.That(true, Is.True);
    }

    [Test]
    public async Task TestDeletePokestop()
    {
        var stopRepo = new PokestopRepository("pokestop", ConnectionString);
        var result = await stopRepo.DeleteAsync("123_test");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TestDeleteRangeCells()
    {
        var cellIds = new List<ulong> { 123, 1234, 12345, 123456 };
        var repo = new CellRepository("s2cell", ConnectionString);
        var result = await repo.DeleteRangeAsync(cellIds);
        Assert.That(result, Is.True);
    }

    private static double BenchmarkAction(Action action, ushort precision = 4)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        action();
        stopwatch.Stop();

        var totalSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, precision);
        Console.WriteLine($"Benchmark took {totalSeconds}s for {action.Method.Name} (Target: {action.Target})");
        return totalSeconds;
    }
}

public class CellRepository : DapperGenericRepository<ulong, Cell>
{
    public CellRepository(string tableName, string connectionString)
        : base(tableName, connectionString)
    {
    }
}

public class PokestopRepository : DapperGenericRepository<string, Pokestop>
{
    public PokestopRepository(string tableName, string connectionString)
        : base(tableName, connectionString)
    {
    }
}