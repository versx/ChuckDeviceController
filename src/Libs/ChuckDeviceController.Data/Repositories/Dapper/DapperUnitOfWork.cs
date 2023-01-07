namespace ChuckDeviceController.Data.Repositories.Dapper;

using Microsoft.Extensions.Configuration;
using MySqlConnector;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Factories;

public class DapperUnitOfWork : IDapperUnitOfWork
{
    #region Variables

    private readonly IMySqlConnectionFactory _factory;
    private readonly string? _connectionString;

    #endregion

    #region Properties

    public IDapperGenericRepository<string, Account> Accounts { get; private set; }

    public IDapperGenericRepository<uint, ApiKey> ApiKeys { get; private set; }

    public IDapperGenericRepository<uint, Assignment> Assignments { get; private set; }

    public IDapperGenericRepository<string, AssignmentGroup> AssignmentGroups { get; private set; }

    public IDapperGenericRepository<string, Device> Devices { get; private set; }

    public IDapperGenericRepository<string, DeviceGroup> DeviceGroups { get; private set; }

    public IDapperGenericRepository<string, Geofence> Geofences { get; private set; }

    public IDapperGenericRepository<string, Instance> Instances { get; private set; }

    public IDapperGenericRepository<string, IvList> IvLists { get; private set; }

    public IDapperGenericRepository<string, Webhook> Webhooks { get; private set; }

    public MySqlTransaction? Transaction => throw new NotImplementedException();

    #endregion

    #region Constructors

    public DapperUnitOfWork(IMySqlConnectionFactory factory, string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        _factory = factory;
        _connectionString = connectionString;

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
    }

    public DapperUnitOfWork(IMySqlConnectionFactory factory, IConfiguration configuration)
        : this(factory, configuration.GetConnectionString("DefaultConnection"))
    {
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

#region Entity Repositories

public class AccountRepository : DapperGenericRepository<string, Account>
{
    public AccountRepository(string connectionString)
        : base(typeof(Account).Name, connectionString)
    {
    }

    public AccountRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class ApiKeyRepository : DapperGenericRepository<uint, ApiKey>
{
    public ApiKeyRepository(string connectionString)
        : base(typeof(ApiKey).Name, connectionString)
    {
    }

    public ApiKeyRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class AssignmentRepository : DapperGenericRepository<uint, Assignment>
{
    public AssignmentRepository(string connectionString)
        : base(typeof(Assignment).Name, connectionString)
    {
    }

    public AssignmentRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class AssignmentGroupRepository : DapperGenericRepository<string, AssignmentGroup>
{
    public AssignmentGroupRepository(string connectionString)
        : base("assignment_group", connectionString) // TODO: Get table from entity
    {
    }

    public AssignmentGroupRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class DeviceRepository : DapperGenericRepository<string, Device>
{
    public DeviceRepository(string connectionString)
        : base(typeof(Device).Name, connectionString)
    {
    }

    public DeviceRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class DeviceGroupRepository : DapperGenericRepository<string, DeviceGroup>
{
    public DeviceGroupRepository(string connectionString)
        : base("device_group", connectionString)
    {
    }

    public DeviceGroupRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class GeofenceRepository : DapperGenericRepository<string, Geofence>
{
    public GeofenceRepository(string connectionString)
        : base(typeof(Geofence).Name, connectionString)
    {
    }

    public GeofenceRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class InstanceRepository : DapperGenericRepository<string, Instance>
{
    public InstanceRepository(string connectionString)
        : base(typeof(Instance).Name, connectionString)
    {
    }

    public InstanceRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class IvListRepository : DapperGenericRepository<string, IvList>
{
    public IvListRepository(string connectionString)
        : base("iv_list", connectionString)
    {
    }

    public IvListRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

public class WebhookRepository : DapperGenericRepository<string, Webhook>
{
    public WebhookRepository(string connectionString)
        : base(typeof(Webhook).Name, connectionString)
    {
    }

    public WebhookRepository(IMySqlConnectionFactory factory)
        : base(factory)
    {
    }
}

#endregion