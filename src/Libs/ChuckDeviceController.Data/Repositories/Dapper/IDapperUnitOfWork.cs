namespace ChuckDeviceController.Data.Repositories.Dapper;

using MySqlConnector;

using ChuckDeviceController.Data.Entities;

/// <summary>
/// 
/// </summary>
public interface IDapperUnitOfWork : IBaseUnitOfWork<MySqlTransaction>
{
    #region Controller Properties

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Account> Accounts { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<uint, ApiKey> ApiKeys { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<uint, Assignment> Assignments { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, AssignmentGroup> AssignmentGroups { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Device> Devices { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, DeviceGroup> DeviceGroups { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Geofence> Geofences { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Instance> Instances { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, IvList> IvLists { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Webhook> Webhooks { get; }

    #endregion

    #region Map Properties

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<ulong, Cell> Cells { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Gym> Gyms { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<ulong, GymDefender> GymDefenders { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, GymTrainer> GymTrainers { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Incident> Incidents { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Pokemon> Pokemon { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, Pokestop> Pokestops { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<ulong, Spawnpoint> Spawnpoints { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<long, Weather> Weather { get; }

    #endregion

    #region Stats Repositories

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, PokemonStats> PokemonStats { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, PokemonIvStats> PokemonIvStats { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, PokemonShinyStats> PokemonShinyStats { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, PokemonHundoStats> PokemonHundoStats { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, RaidStats> RaidStats { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, QuestStats> QuestStats { get; }

    /// <summary>
    /// 
    /// </summary>
    IDapperGenericRepository<string, InvasionStats> InvasionStats { get; }

    #endregion
}
