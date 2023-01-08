namespace ChuckDeviceController.Data.Repositories.Dapper;

using MySqlConnector;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Factories;

public class DapperUnitOfWork : IDapperUnitOfWork
{
    #region Variables

    private readonly IMySqlConnectionFactory _factory;

    #endregion

    #region Properties

    #region Controller Entity Repositories

    public IDapperGenericRepository<string, Account> Accounts { get; }

    public IDapperGenericRepository<uint, ApiKey> ApiKeys { get; }

    public IDapperGenericRepository<uint, Assignment> Assignments { get; }

    public IDapperGenericRepository<string, AssignmentGroup> AssignmentGroups { get; }

    public IDapperGenericRepository<string, Device> Devices { get; }

    public IDapperGenericRepository<string, DeviceGroup> DeviceGroups { get; }

    public IDapperGenericRepository<string, Geofence> Geofences { get; }

    public IDapperGenericRepository<string, Instance> Instances { get; }

    public IDapperGenericRepository<string, IvList> IvLists { get; }

    public IDapperGenericRepository<string, Webhook> Webhooks { get; }

    #endregion

    #region Data Entity Repositories

    public IDapperGenericRepository<ulong, Cell> Cells { get; }

    public IDapperGenericRepository<string, Gym> Gyms { get; }

    public IDapperGenericRepository<ulong, GymDefender> GymDefenders { get; }

    public IDapperGenericRepository<string, GymTrainer> GymTrainers { get; }

    public IDapperGenericRepository<string, Incident> Incidents { get; }

    public IDapperGenericRepository<string, Pokemon> Pokemon { get; }

    public IDapperGenericRepository<string, Pokestop> Pokestops { get; }

    public IDapperGenericRepository<ulong, Spawnpoint> Spawnpoints { get; }

    public IDapperGenericRepository<long, Weather> Weather { get; }

    #endregion

    public MySqlTransaction? Transaction => throw new NotImplementedException();

    #endregion

    #region Constructors

    public DapperUnitOfWork(IMySqlConnectionFactory factory)
    {
        _factory = factory;

        Accounts = new AccountRepository(_factory);
        ApiKeys = new ApiKeyRepository(_factory);
        Assignments = new AssignmentRepository(_factory);
        AssignmentGroups = new AssignmentGroupRepository(_factory);
        Devices = new DeviceRepository(_factory);
        DeviceGroups = new DeviceGroupRepository(_factory);
        Geofences = new GeofenceRepository(_factory);
        Instances = new InstanceRepository(_factory);
        IvLists = new IvListRepository(_factory);
        Webhooks = new WebhookRepository(_factory);

        Cells = new CellRepository(_factory);
        Gyms = new GymRepository(_factory);
        GymDefenders = new GymDefenderRepository(_factory);
        GymTrainers = new GymTrainerRepository(_factory);
        Incidents = new IncidentRepository(_factory);
        Pokemon = new PokemonRepository(_factory);
        Pokestops = new PokestopRepository(_factory);
        Spawnpoints = new SpawnpointRepository(factory);
        Weather = new WeatherRepository(factory);
    }

    #endregion

    #region Public Methods

    public MySqlTransaction BeginTransaction()
    {
        throw new NotImplementedException();
    }

    public Task<MySqlTransaction> BeginTransactionAsync(CancellationToken stoppingToken = default)
    {
        throw new NotImplementedException();
    }

    public bool Commit()
    {
        throw new NotImplementedException();
    }

    public Task<bool> CommitAsync(CancellationToken stoppingToken = default)
    {
        throw new NotImplementedException();
    }

    public void Rollback()
    {
        throw new NotImplementedException();
    }

    public Task RollbackAsync(CancellationToken stoppingToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion
}

#region Controller Entity Repositories

public class AccountRepository : DapperGenericRepository<string, Account>
{
    public AccountRepository(IMySqlConnectionFactory factory)
        : base("account", factory)
    {
    }
}

public class ApiKeyRepository : DapperGenericRepository<uint, ApiKey>
{
    public ApiKeyRepository(IMySqlConnectionFactory factory)
        : base("api_key", factory)
    {
    }
}

public class AssignmentRepository : DapperGenericRepository<uint, Assignment>
{
    public AssignmentRepository(IMySqlConnectionFactory factory)
        : base("assignment", factory)
    {
    }
}

public class AssignmentGroupRepository : DapperGenericRepository<string, AssignmentGroup>
{
    public AssignmentGroupRepository(IMySqlConnectionFactory factory)
        : base("assignment_group", factory)
    {
    }
}

public class DeviceRepository : DapperGenericRepository<string, Device>
{
    public DeviceRepository(IMySqlConnectionFactory factory)
        : base("device", factory)
    {
    }
}

public class DeviceGroupRepository : DapperGenericRepository<string, DeviceGroup>
{
    public DeviceGroupRepository(IMySqlConnectionFactory factory)
        : base("device_group", factory)
    {
    }
}

public class GeofenceRepository : DapperGenericRepository<string, Geofence>
{
    public GeofenceRepository(IMySqlConnectionFactory factory)
        : base("geofence", factory)
    {
    }
}

public class InstanceRepository : DapperGenericRepository<string, Instance>
{
    public InstanceRepository(IMySqlConnectionFactory factory)
        : base("instance", factory)
    {
    }
}

public class IvListRepository : DapperGenericRepository<string, IvList>
{
    public IvListRepository(IMySqlConnectionFactory factory)
        : base("iv_list", factory)
    {
    }
}

public class WebhookRepository : DapperGenericRepository<string, Webhook>
{
    public WebhookRepository(IMySqlConnectionFactory factory)
        : base("webhook", factory)
    {
    }
}

#endregion

#region Map Entity Repositories

public class CellRepository : DapperGenericRepository<ulong, Cell>
{
    public CellRepository(IMySqlConnectionFactory factory)
        : base("s2cell", factory)
    {
    }
}

public class GymRepository : DapperGenericRepository<string, Gym>
{
    public GymRepository(IMySqlConnectionFactory factory)
        : base("gym", factory)
    {
    }
}

public class GymDefenderRepository : DapperGenericRepository<ulong, GymDefender>
{
    public GymDefenderRepository(IMySqlConnectionFactory factory)
        : base("gym_defender", factory)
    {
    }
}

public class GymTrainerRepository : DapperGenericRepository<string, GymTrainer>
{
    public GymTrainerRepository(IMySqlConnectionFactory factory)
        : base("gym_trainer", factory)
    {
    }
}

public class IncidentRepository : DapperGenericRepository<string, Incident>
{
    public IncidentRepository(IMySqlConnectionFactory factory)
        : base("incident", factory)
    {
    }
}

public class PokemonRepository : DapperGenericRepository<string, Pokemon>
{
    public PokemonRepository(IMySqlConnectionFactory factory)
        : base("pokemon", factory)
    {
    }
}

public class PokestopRepository : DapperGenericRepository<string, Pokestop>
{
    public PokestopRepository(IMySqlConnectionFactory factory)
        : base("pokestop", factory)
    {
    }
}

public class SpawnpointRepository : DapperGenericRepository<ulong, Spawnpoint>
{
    public SpawnpointRepository(IMySqlConnectionFactory factory)
        : base("spawnpoint", factory)
    {
    }
}

public class WeatherRepository : DapperGenericRepository<long, Weather>
{
    public WeatherRepository(IMySqlConnectionFactory factory)
        : base("weather", factory)
    {
    }
}

#endregion