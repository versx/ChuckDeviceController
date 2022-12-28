namespace ChuckDeviceController.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.Dapper;
using ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

// Reference: https://github.com/timschreiber/DapperUnitOfWork
// Reference: https://c-sharpcorner.com/article/implement-unit-of-work-and-generic-repository-pattern-in-a-web-api-net-core-pro/

public class UnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
    #region Variables

    private readonly TDbContext _context;

    #endregion

    #region Properties

    public IGenericEntityRepository<Account> Accounts { get; }

    public IGenericEntityRepository<ApiKey> ApiKeys { get; }

    public IGenericEntityRepository<Assignment> Assignments { get; }

    public IGenericEntityRepository<AssignmentGroup> AssignmentGroups { get; }

    public IGenericEntityRepository<Device> Devices { get; }

    public IGenericEntityRepository<DeviceGroup> DeviceGroups { get; }

    public IGenericEntityRepository<Geofence> Geofences { get; }

    public IGenericEntityRepository<Instance> Instances { get; }

    public IGenericEntityRepository<IvList> IvLists { get; }

    public IGenericEntityRepository<Webhook> Webhooks { get; }


    //public IPokemonRepository Pokemon { get; }

    //public IPokestopRepository Pokestops { get; }

    //public IIncidentRepository Incidents { get; }

    //public IGymRepository Gyms { get; }

    //public IGymDefenderRepository GymDefenders { get; }

    //public IGymTrainerRepository GymTrainers { get; }

    //public ICellRepository Cells { get; }

    //public IWeatherRepository Weather { get; }

    //public ISpawnpointRepository Spawnpoints { get; }

    public IDbContextTransaction? Transaction => _context.Database.CurrentTransaction;

    #endregion

    #region Constructor / Deconstructor

    public UnitOfWork(TDbContext context)
    {
        _context = context;

        // Controller entity repositories
        Accounts = new GenericEntityRepository<TDbContext, Account>(_context);
        ApiKeys = new GenericEntityRepository<TDbContext, ApiKey>(_context);
        Assignments = new GenericEntityRepository<TDbContext, Assignment>(_context);
        AssignmentGroups = new GenericEntityRepository<TDbContext, AssignmentGroup>(_context);
        Devices = new GenericEntityRepository<TDbContext, Device>(_context);
        DeviceGroups = new GenericEntityRepository<TDbContext, DeviceGroup>(_context);
        Geofences = new GenericEntityRepository<TDbContext, Geofence>(_context);
        Instances = new GenericEntityRepository<TDbContext, Instance>(_context);
        IvLists = new GenericEntityRepository<TDbContext, IvList>(_context);
        Webhooks = new GenericEntityRepository<TDbContext, Webhook>(_context);

        // Map entity repositories
        //Pokemon = new PokemonRepository(_context);
        //Pokestops = new PokestopRepository(_context);
        //Incidents = new IncidentRepository(_context);
        //Gyms = new GymRepository(_context);
        //GymDefenders = new GymDefenderRepository(_context);
        //GymTrainers = new GymTrainerRepository(_context);
        //Cells = new CellRepository(_context);
        //Weather = new WeatherRepository(_context);
        //Spawnpoints = new SpawnpointRepository(_context);
    }

    ~UnitOfWork()
    {
        Dispose();
    }

    #endregion

    #region Public Methods

    public IDbContextTransaction BeginTransaction()
    {
        return _context.Database.BeginTransaction();
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken stoppingToken = default)
    {
        return await _context.Database.BeginTransactionAsync(stoppingToken);
    }

    public bool Commit()
    {
        try
        {
            _context.Database.CommitTransaction();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CommitAsync(CancellationToken stoppingToken = default)
    {
        try
        {
            //await _context.Database.CommitTransactionAsync(stoppingToken);
            //return true;
            var rowsAffected = await _context.SaveChangesAsync(stoppingToken);
            var result = rowsAffected > 0;
            return result;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public void Rollback()
    {
        _context.Database.RollbackTransaction();
    }

    public async Task RollbackAsync(CancellationToken stoppingToken = default)
    {
        await _context.Database.RollbackTransactionAsync(stoppingToken);
    }

    public void Dispose()
    {
        _context.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}

public class UnitOfWorkDapper : IUnitOfWorkDapper
{
    #region Variables

    private readonly MySqlConnection? _connection;
    private MySqlTransaction? _transaction;

    #endregion

    #region Properties

    public IGenericEntityRepository<Account> Accounts { get; }

    public IGenericEntityRepository<ApiKey> ApiKeys { get; }

    public IGenericEntityRepository<Assignment> Assignments { get; }

    public IGenericEntityRepository<AssignmentGroup> AssignmentGroups { get; }

    public IGenericEntityRepository<Device> Devices { get; }

    public IGenericEntityRepository<DeviceGroup> DeviceGroups { get; }

    public IGenericEntityRepository<Geofence> Geofences { get; }

    public IGenericEntityRepository<Instance> Instances { get; }

    public IGenericEntityRepository<IvList> IvLists { get; }

    public IGenericEntityRepository<Webhook> Webhooks { get; }


    //public IPokemonRepository Pokemon { get; }

    //public IPokestopRepository Pokestops { get; }

    //public IIncidentRepository Incidents { get; }

    //public IGymRepository Gyms { get; }

    //public IGymDefenderRepository GymDefenders { get; }

    //public IGymTrainerRepository GymTrainers { get; }

    //public ICellRepository Cells { get; }

    //public IWeatherRepository Weather { get; }

    //public ISpawnpointRepository Spawnpoints { get; }

    public MySqlTransaction? Transaction => _transaction;

    #endregion

    #region Constructor / Deconstructor

    public UnitOfWorkDapper(MySqlConnection connection)
    {
        _connection = connection;

        // Controller entity repositories
        Accounts = new GenericEntityDapperRepository<Account>(_connection);
        ApiKeys = new GenericEntityDapperRepository<ApiKey>(_connection);
        Assignments = new GenericEntityDapperRepository<Assignment>(_connection);
        AssignmentGroups = new GenericEntityDapperRepository<AssignmentGroup>(_connection);
        Devices = new GenericEntityDapperRepository<Device>(_connection);
        DeviceGroups = new GenericEntityDapperRepository<DeviceGroup>(_connection);
        Geofences = new GenericEntityDapperRepository<Geofence>(_connection);
        Instances = new GenericEntityDapperRepository<Instance>(_connection);
        IvLists = new GenericEntityDapperRepository<IvList>(_connection);
        Webhooks = new GenericEntityDapperRepository<Webhook>(_connection);

        // Map entity repositories
        //Pokemon = new PokemonRepository(_context);
        //Pokestops = new PokestopRepository(_context);
        //Incidents = new IncidentRepository(_context);
        //Gyms = new GymRepository(_context);
        //GymDefenders = new GymDefenderRepository(_context);
        //GymTrainers = new GymTrainerRepository(_context);
        //Cells = new CellRepository(_context);
        //Weather = new WeatherRepository(_context);
        //Spawnpoints = new SpawnpointRepository(_context);
    }

    ~UnitOfWorkDapper()
    {
        Dispose();
    }

    #endregion

    #region Public Methods

    public MySqlTransaction BeginTransaction()
    {
        return _transaction = _connection!.BeginTransaction();
    }

    public async Task<MySqlTransaction> BeginTransactionAsync(CancellationToken stoppingToken = default)
    {
        return _transaction = await _connection!.BeginTransactionAsync(stoppingToken);
    }

    public bool Commit()
    {
        try
        {
            _transaction!.Commit();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CommitAsync(CancellationToken stoppingToken = default)
    {
        try
        {
            await _transaction!.CommitAsync(stoppingToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Rollback()
    {
        _transaction!.Rollback();
    }

    public async Task RollbackAsync(CancellationToken stoppingToken = default)
    {
        await _transaction!.RollbackAsync(stoppingToken);
    }

    public void Dispose()
    {
        if (_transaction != null)
        {
            _transaction.Dispose();
        }
        _transaction = null!;

        GC.SuppressFinalize(this);
    }

    #endregion
}