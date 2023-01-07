namespace ChuckDeviceController.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using ChuckDeviceController.Data.Entities;
using ChuckDeviceController.Data.Repositories.EntityFrameworkCore;

// Reference: https://github.com/timschreiber/DapperUnitOfWork
// Reference: https://c-sharpcorner.com/article/implement-unit-of-work-and-generic-repository-pattern-in-a-web-api-net-core-pro/
// Reference: https://dejanstojanovic.net/aspnet/2021/november/unit-of-work-pattern-with-dapper/

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


    //public IGenericEntityRepository<Pokemon> Pokemon { get; }

    //public IGenericEntityRepository<Pokestop> Pokestops { get; }

    //public IGenericEntityRepository<Incident> Incidents { get; }

    //public IGenericEntityRepository<Gym> Gyms { get; }

    //public IGenericEntityRepository<GymDefender> GymDefenders { get; }

    //public IGenericEntityRepository<GymTrainer> GymTrainers { get; }

    //public IGenericEntityRepository<Cell> Cells { get; }

    //public IGenericEntityRepository<Weather> Weather { get; }

    //public IGenericEntityRepository<Spawnpoint> Spawnpoints { get; }

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
        catch //(Exception ex)
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