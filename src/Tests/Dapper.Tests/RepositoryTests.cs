namespace Dapper.Tests;

using System.Diagnostics;

using MicroOrm.Dapper.Repositories.Config;
using MicroOrm.Dapper.Repositories.SqlGenerator;

using ChuckDeviceController.Data.Common;
using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Factories;
using ChuckDeviceController.Data.Repositories;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Data.TypeHandlers;
using ChuckDeviceController.Extensions;

internal class RepositoryTests
{
    private const string ConnectionString = "";

    [SetUp]
    public void Setup()
    {
        //EntityDataRepository.SetTypeMap<Account>();
        //EntityDataRepository.SetTypeMap<ApiKey>();
        //EntityDataRepository.SetTypeMap<Assignment>();
        //EntityDataRepository.SetTypeMap<AssignmentGroup>();
        //EntityDataRepository.SetTypeMap<Device>();
        //EntityDataRepository.SetTypeMap<DeviceGroup>();
        //EntityDataRepository.SetTypeMap<Geofence>();
        //EntityDataRepository.SetTypeMap<Instance>();
        //EntityDataRepository.SetTypeMap<IvList>();
        //EntityDataRepository.SetTypeMap<Webhook>();

        EntityDataRepository.SetTypeMap<Cell>();
        EntityDataRepository.SetTypeMap<Pokestop>();

        ////EntityDataRepository.AddTypeMappers();
        SqlMapper.AddTypeHandler(typeof(InstanceTypeTypeHandler), InstanceTypeTypeHandler.Default);
        //SqlMapper.AddTypeHandler(typeof(WebhookTypeTypeHandler), WebhookTypeTypeHandler.Default);
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<string>>()); // Instance.Geofences / Webhook.Geofences / IvList.PokemonIds
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<uint>>()); // AssignmentGroup.AssignmentIds
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<WebhookType>>()); // Webhook.Types
        SqlMapper.AddTypeHandler(new JsonTypeHandler<GeofenceData>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<InstanceData>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<WebhookData>());
    }

    [Test]
    public async Task DapperExtensionsRepositoryTests()
    {
        MicroOrmConfig.SqlProvider = SqlProvider.MySQL;
        MicroOrmConfig.AllowKeyAsIdentity = true;
        MicroOrmConfig.UseQuotationMarks = true;

        var factory = new MySqlConnectionFactory(ConnectionString);

        var deviceRepository = new BaseEntityRepository<Device>(factory);
        var device = await deviceRepository.FindByIdAsync("atv08");
        var devices = await deviceRepository.FindAllAsync();

        var instanceRepository = new BaseEntityRepository<Instance>(factory);
        var instance = await instanceRepository.FindByIdAsync("0Findy");

        var webhookRepository = new BaseEntityRepository<Webhook>(factory);
        //var webhook = await webhookRepository.FindAsync(x => x.Data != null);
        var webhook = await webhookRepository.FindByIdAsync("TestTest");

        var ivListRepository = new BaseEntityRepository<IvList>(factory);
        //var ivList = await ivListRepository.FindAsync(x => x.PokemonIds.Count > 0);
        var ivList = await ivListRepository.FindAsync(x => x.PokemonIds != null);

        Assert.Pass();
    }

    [Test]
    public async Task GenericDapperRepositoryTests()
    {
        EntityDataRepository.SetTypeMap<Account>();
        EntityDataRepository.SetTypeMap<ApiKey>();
        EntityDataRepository.SetTypeMap<Assignment>();
        EntityDataRepository.SetTypeMap<AssignmentGroup>();
        EntityDataRepository.SetTypeMap<Device>();
        EntityDataRepository.SetTypeMap<DeviceGroup>();
        EntityDataRepository.SetTypeMap<Geofence>();
        EntityDataRepository.SetTypeMap<Instance>();
        EntityDataRepository.SetTypeMap<IvList>();
        EntityDataRepository.SetTypeMap<Webhook>();

        var factory = new MySqlConnectionFactory(ConnectionString);

        var deviceRepository = new DeviceRepository(factory);
        var device = await deviceRepository.FindAsync("atv08");
        var devices = await deviceRepository.FindAllAsync();

        var instanceRepository = new InstanceRepository(factory);
        var instance = await instanceRepository.FindAsync("0Findy");

        var webhookRepository = new WebhookRepository(factory);
        //var webhook = await webhookRepository.FindAsync(x => x.Data != null);
        var webhook = await webhookRepository.FindAsync("TestTest");

        var ivListRepository = new IvListRepository(factory);
        var ivList = await ivListRepository.FindAsync(x => x.PokemonIds.Count > 0);
        var anyIvList = await ivListRepository.FindAsync(x => x.PokemonIds.Any());
        var notAnyIvList = await ivListRepository.FindAsync(x => !x.PokemonIds.Any());
        var hasCountIvList = await ivListRepository.FindAsync(x => x.PokemonIds.Count() > 0);
        var noCountIvList = await ivListRepository.FindAsync(x => x.PokemonIds.Count() == 0);

        var accountRepository = new AccountRepository(factory);
        var cleanAccounts = await accountRepository.FindAsync(x => string.IsNullOrEmpty(x.Failed));
        var failedAccounts = await accountRepository.FindAsync(x => !string.IsNullOrEmpty(x.Failed));

        Assert.Pass();
    }

    #region Controller Entity Tests

    [Test]
    public async Task TestSelectAccounts()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var accounts = await uow.Accounts.FindAsync(x => x.Failed == null && x.FirstWarningTimestamp == null && x.Level > 30);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectApiKeys()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var apiKeys = await uow.ApiKeys.FindAsync(x => x.Scope > 0);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectAssignments()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var assignments = await uow.Assignments.FindAsync(x => x.Time > 0);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectAssignmentGroups()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var assignmentGroups = await uow.AssignmentGroups.FindAsync(x => x.Enabled);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectDevices()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var devices = await uow.Devices.FindAsync(x => x.InstanceName != null);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectDeviceGroups()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var deviceGroups = await uow.DeviceGroups.FindAsync(x => x.DeviceUuids.Count > 0);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectGeofences()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var geofences = await uow.Geofences.FindAsync(x => x.Type == GeofenceType.Circle);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectInstances()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var instances = await uow.Instances.FindAsync(x => x.Type == InstanceType.AutoQuest);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectIvLists()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        var ivLists = await uow.IvLists.FindAsync(x => x.PokemonIds.Count > 0);

        Assert.Pass();
    }

    [Test]
    public async Task TestSelectWebhooks()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var uow = new DapperUnitOfWork(factory);
        //var webhooks = await uow.Webhooks.FindAsync(x => x.Data.PokemonIds.Count > 0);
        var webhooks = await uow.Webhooks.FindAsync(x => x.Data != null);

        Assert.Pass();
    }

    #endregion

    #region Map Entity Tests

    [Test]
    public async Task TestSelectCell()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var cellRepo = new CellRepository(factory);
        var cell = await cellRepo.FindAsync(123);

        Assert.That(cell, Is.Not.Null);
    }

    [Test]
    public async Task TestInsertCell()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var now = DateTime.UtcNow.ToTotalSeconds();
        var sw = new Stopwatch();

        sw.Start();
        var repo = new CellRepository(factory);
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
        var factory = new MySqlConnectionFactory(ConnectionString);
        var now = DateTime.UtcNow.ToTotalSeconds();
        var repo = new CellRepository(factory);
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
        var factory = new MySqlConnectionFactory(ConnectionString);
        var now = DateTime.UtcNow.ToTotalSeconds();
        var sw = new Stopwatch();

        sw.Start();
        var stopRepo = new PokestopRepository(factory);
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
        var factory = new MySqlConnectionFactory(ConnectionString);
        var repo = new CellRepository(factory);
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
        var factory = new MySqlConnectionFactory(ConnectionString);
        var now = DateTime.UtcNow.ToTotalSeconds();
        var timeTaken = BenchmarkAction(async () =>
        {
            var stopRepo = new PokestopRepository(factory);
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
        Assert.Pass();
    }

    [Test]
    public void TestUpdatePokestopWithAllProperties()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var now = DateTime.UtcNow.ToTotalSeconds();
        var timeTaken = BenchmarkAction(async () =>
        {
            var stopRepo = new PokestopRepository(factory);
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

        Assert.Pass();
    }

    [Test]
    public async Task TestDeletePokestop()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var stopRepo = new PokestopRepository(factory);
        var result = await stopRepo.DeleteAsync("123_test");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TestDeleteRangeCells()
    {
        var factory = new MySqlConnectionFactory(ConnectionString);
        var cellIds = new List<ulong> { 123, 1234, 12345, 123456 };
        var repo = new CellRepository(factory);
        var result = await repo.DeleteRangeAsync(cellIds);
        Assert.That(result, Is.True);
    }

    #endregion

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